using Org.Mentalis.Security.Ssl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharedProtocol.Compression.HeadersDeltaCompression;
using SharedProtocol.Exceptions;
using SharedProtocol.Extensions;

namespace SharedProtocol.Compression.Http2DeltaHeadersCompression
{
    //This headers compression algorithm is described in
    // http://tools.ietf.org/html/draft-ietf-httpbis-header-compression-01
    /// <summary>
    /// This class implement header compression.
    /// </summary>
    public class CompressionProcessor : ICompressionProcessor
    {
        private const int HeadersLimit = 200;
        private const int MaxHeaderByteSize = 4096;

        private readonly SizedHeadersList _localHeaderTable;
        private readonly SizedHeadersList _remoteHeaderTable;
        private SizedHeadersList _localRefSet;
        private SizedHeadersList _remoteRefSet;

        private MemoryStream _serializerStream;

        public CompressionProcessor(ConnectionEnd end)
        {
            if (end == ConnectionEnd.Client)
            {
                _localHeaderTable = CompressionInitialHeaders.ResponseInitialHeaders;
                _remoteHeaderTable = CompressionInitialHeaders.RequestInitialHeaders;
            }
            else
            {
                _localHeaderTable = CompressionInitialHeaders.RequestInitialHeaders;
                _remoteHeaderTable = CompressionInitialHeaders.ResponseInitialHeaders;
            }
            _localRefSet = new SizedHeadersList();
            _remoteRefSet = new SizedHeadersList();

            InitCompressor();
            InitDecompressor();
        }

        private void InitCompressor()
        {
            _serializerStream = new MemoryStream();
        }

        private void InitDecompressor()
        {
            _currentOffset = 0;
        }

        private void ModifyTable(string headerName, string headerValue, IndexationType headerType,
                                        SizedHeadersList useHeadersTable, int index)
        {
            int headerLen = headerName.Length + headerValue.Length;
                switch (headerType)
                {
                    case IndexationType.Incremental:
                        if (useHeadersTable.Count > HeadersLimit - 1)
                        {
                            useHeadersTable.RemoveAt(0);
                        }

                        while (useHeadersTable.StoredHeadersSize + headerLen > MaxHeaderByteSize)
                        {
                            useHeadersTable.RemoveAt(0);
                        }
                        useHeadersTable.Add(new KeyValuePair<string, string>(headerName, headerValue));
                        break;
                    case IndexationType.Substitution:
                        if (index != -1)
                        {
                            useHeadersTable[index] = new KeyValuePair<string, string>(headerName, headerValue);
                        }
                        else
                        {
                            if (useHeadersTable.Count > HeadersLimit - 1)
                            {
                                useHeadersTable.RemoveAt(0);
                            }

                            while (useHeadersTable.StoredHeadersSize + headerLen > MaxHeaderByteSize)
                            {
                                useHeadersTable.RemoveAt(0);
                            }
                            //If header wasn't found then add it to the table
                            useHeadersTable.Add(new KeyValuePair<string, string>(headerName, headerValue));
                        }
                        break;
                    default:
                        return;
                }
        }

        #region Compression

        private void CompressHeader(KeyValuePair<string, string> header, IAdditionalHeaderInfo type)
        {
            byte prefix = 0;
            var headerType = (type as Indexation).Type;

            switch (headerType)
            {
                case IndexationType.WithoutIndexation:
                case IndexationType.Incremental:
                    prefix = 5;
                    break;
                case IndexationType.Substitution:
                    prefix = 6;
                    break;
                case IndexationType.Indexed:
                    CompressIndexed(header);
                    return;
            }

            CompressNonIndexed(header.Key, header.Value, headerType, prefix);
        }

        private void CompressNonIndexed(string headerName, string headerValue, IndexationType headerType, byte prefix)
        {
            int index = _remoteHeaderTable.FindIndex(kv => kv.Key == headerName);

            byte nameLenBinary = 0; // headers cant be more then 255 characters length
            byte[] nameBinary = new byte[0];

            //It's necessary to form result array because partial writeToOutput stream can cause problems because of multithreading
            using (var stream = new MemoryStream(64))
            {
                byte[] indexBinary;
                byte valueLenBinary;
                byte[] valueBinary;

                if (index != -1)
                {
                    indexBinary = (index + 1).ToUVarInt(prefix);
                }
                else
                {
                    indexBinary = 0.ToUVarInt(prefix);
                    nameBinary = Encoding.UTF8.GetBytes(headerName);
                    nameLenBinary = (byte)nameBinary.Length;
                }

                //Set without index type
                indexBinary[0] |= (byte)headerType;

                valueBinary = Encoding.UTF8.GetBytes(headerValue);
                valueLenBinary = (byte)valueBinary.Length;

                stream.Write(indexBinary, 0, indexBinary.Length);

                //write replaced index. It's equal with the found index in our case
                if (headerType == IndexationType.Substitution)
                {
                    stream.Write(indexBinary, 0, indexBinary.Length);
                }

                if (index == -1)
                {
                    stream.WriteByte(nameLenBinary);
                    stream.Write(nameBinary, 0, nameBinary.Length);
                }

                stream.WriteByte(valueLenBinary);
                stream.Write(valueBinary, 0, valueBinary.Length);

                WriteToOutput(stream.GetBuffer(), 0, (int)stream.Position);
            }

            ModifyTable(headerName, headerValue, headerType, _remoteHeaderTable, index);
        }

        private void CompressIndexed(KeyValuePair<string, string> header)
        {
            int index = _remoteHeaderTable.IndexOf(header);
            const byte prefix = 7;
            var bytes = index.ToUVarInt(prefix);

            //Set indexed type
            bytes[0] |= (byte) IndexationType.Indexed;

            WriteToOutput(bytes, 0, bytes.Length);
        }

        //Method retypes as many headers as it can to be Indexed
        //and checks if headers marked as indexed are present in the headers table
        /*private void OptimizeInputAndSendOptimized(List<KeyValuePair<string, string>> headers)
        {
            for (int i = 0; i < headers.Count; i++ )
            {
                var headerKv = new KeyValuePair<string, string>(headers[i].Item1, headers[i].Item2);
                IndexationType headerType = (headers[i].Item3 as Indexation).Type;

                int index = _remoteHeaderTable.IndexOf(headerKv);

                //case headerType == IndexationType.Incremental
                //must not be considered because headers table can contain duplicates
                if (index != -1 && headerType == IndexationType.Substitution)
                {
                    CompressIndexed(headerKv);
                    headers.Remove(headers[i--]);
                }

                //If header marked as indexed, but not found in the table, compress it as incremental.
                if (index == -1 && headerType == IndexationType.Indexed)
                {
                    CompressNonIndexed(headerKv.Key, headerKv.Value, IndexationType.Incremental, 5);
                    headers.Remove(headers[i--]);
                }
            }
        }*/

        public byte[] Compress(HeadersList headers)
        {
            var toSend = new SizedHeadersList();
            var toDelete = new SizedHeadersList(_remoteRefSet);
            ClearStream(_serializerStream, (int) _serializerStream.Position);

            //OptimizeInputAndSendOptimized(headersCopy); - dont need this?

            foreach (var header in headers)
            {
                if (header.Key == null || header.Value == null)
                {
                    throw new InvalidHeaderException(header);
                }
                if (!_remoteRefSet.Contains(header))
                {
                    //Not there, Will send
                    toSend.Add(header);
                }
                else
                {
                    //Already there, don't delete
                    toDelete.Remove(header);
                }
            }
            foreach (var header in toDelete)
            {
                //Anything left in toDelete, should send, so it is deleted from ref set.
                CompressIndexed(header);
                _remoteRefSet.Remove(header); //Update our copy
            }
            foreach (var header in toSend)
            {
                //Send whatever was left in headersCopy
                if (_remoteHeaderTable.Contains(header))
                {
                    CompressIndexed(header);
                }
                else
                {
                    CompressHeader(header, new Indexation(IndexationType.Incremental));
                }
                _remoteRefSet.Add(header); //Update our copy
            }

            _serializerStream.Flush();
            var result = new byte[_serializerStream.Position];
            var streamBuffer = _serializerStream.GetBuffer();
            Buffer.BlockCopy(streamBuffer, 0, result, 0, (int)_serializerStream.Position);
            return result;
        }

        #endregion

        #region Decompression

        private int _currentOffset;

        private Tuple<string, string, IndexationType> ParseHeader(byte[] bytes)
        {
            var type = GetHeaderType(bytes);
            int index = GetIndex(bytes, type);
            string name;
            string value;
            byte valueLen;
            byte nameLen;

            switch (type)
            {
                case IndexationType.Indexed:
                    var kv = _localHeaderTable[index];
                    return new Tuple<string, string, IndexationType>(kv.Key, kv.Value, type);
                case IndexationType.Incremental:
                case IndexationType.WithoutIndexation:
                case IndexationType.Substitution:
                    //get replaced entry index. It's equal with the found index in our case
                    if (type == IndexationType.Substitution)
                    {
                        index = GetIndex(bytes, type);
                    }
                    if (index == 0)
                    {
                        nameLen = bytes[_currentOffset++];
                        name = Encoding.UTF8.GetString(bytes, _currentOffset, nameLen);
                        _currentOffset += nameLen;
                    }
                    else
                    {
                        //Index increased by 1 was sent
                        name = _localHeaderTable[index - 1].Key;
                    }
                    valueLen = bytes[_currentOffset++];
                    value = Encoding.UTF8.GetString(bytes, _currentOffset, valueLen);
                    _currentOffset += valueLen;

                    ModifyTable(name, value, type, _localHeaderTable, index - 1);

                    return new Tuple<string, string, IndexationType>(name, value, type);
            }

            return default(Tuple<string, string, IndexationType>);
        }
        
        private int GetIndex(byte[] bytes, IndexationType type)
        {
            byte prefix = 0;
            byte firstByteValue = /*(byte) (*/bytes[_currentOffset];// & (~(byte)type));

            switch (type)
            {  
                case IndexationType.Incremental:
                case IndexationType.WithoutIndexation:
                    prefix = 5;
                    break;
                case IndexationType.Substitution:
                    prefix = 6;
                    break;
                case IndexationType.Indexed:
                    prefix = 7;
                    break;
            }
            int maxPrefixVal = (1 << prefix) - 1;

            if (firstByteValue < maxPrefixVal)
            {
                _currentOffset++;
                return firstByteValue;
            }

            int i = 1;
            while (true)
            {
                if ((bytes[_currentOffset + i] & 0x80) == 0)
                {
                    break;
                }
                i++;
            }

            var numberBytes = new byte[++i];
            Buffer.BlockCopy(bytes, _currentOffset, numberBytes, 0, i);
            _currentOffset += i;

            return new int().FromUVarInt(numberBytes);
        }

        private IndexationType GetHeaderType(byte[] bytes)
        {
            var typeByte = bytes[_currentOffset];
            IndexationType indexationType;
            
            if ((typeByte & 0x80) == (byte)IndexationType.Indexed)
            {
                indexationType = IndexationType.Indexed;
            }
            else if ((typeByte & 0x60) == (byte)IndexationType.WithoutIndexation)
            {
                indexationType = IndexationType.WithoutIndexation;
            }
            else if ((typeByte & 0x40) == (byte)IndexationType.Incremental)
            {
                indexationType = IndexationType.Incremental;
            }
            else 
            {
                indexationType = IndexationType.Substitution;
            }
            //throw type mask away
            bytes[_currentOffset] = (byte)(bytes[_currentOffset] & (~(byte)indexationType));
            return indexationType;
        }

        public HeadersList Decompress(byte[] serializedHeaders)
        {
            try
            {
                SizedHeadersList workingSet = new SizedHeadersList(_localRefSet);

                _currentOffset = 0;

                while (_currentOffset != serializedHeaders.Length)
                {
                    var entry = ParseHeader(serializedHeaders);
                    var header = new KeyValuePair<string, string>(entry.Item1, entry.Item2);

                    if (entry.Item3 != IndexationType.WithoutIndexation)
                    {
                        workingSet.Add(header);
                    }
                    else
                    {
                        if (workingSet.Contains(header))
                            workingSet.RemoveAll(h => h.Equals(header));
                        else
                            workingSet.Add(header);
                    }
                }

                _localRefSet = new SizedHeadersList(workingSet);

                for (int i = _localRefSet.Count - 1; i >= 0; --i)
                {
                    var header = _localRefSet[i];
                    if (!_localHeaderTable.Contains(header))
                        _localRefSet.RemoveAll(h => h.Equals(header));
                }

                return workingSet;
            }
            catch (Exception e)
            {
                throw new CompressionError(e);
            }
        }

        #endregion

        private void WriteToOutput(byte[] bytes, int offset, int length)
        {
            _serializerStream.Write(bytes, offset, length);
        }

        private void ClearStream(Stream input, int len)
        {
            var buffer = new byte[len];
            input.Position = 0;
            input.Write(buffer, 0, len);
            input.SetLength(0);
        }

        public void Dispose()
        {
            _serializerStream.Dispose();
        }

    }
}
