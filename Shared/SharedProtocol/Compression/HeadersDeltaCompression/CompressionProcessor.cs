using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharedProtocol.Exceptions;
using SharedProtocol.Extensions;
using SharedProtocol.Compression;

namespace SharedProtocol.Http2HeadersCompression
{
    //This headers compression algorithm is described in
    // https://github.com/yoavnir/compression-spec/blob/7f67f0dbecdbe65bc22f3e3b57e2d5adefeb08dd/compression-spec.txt
    public class CompressionProcessor : ICompressionProcessor
    {
        private readonly List<KeyValuePair<string, string>> _requestHeadersStorage;
        private readonly List<KeyValuePair<string, string>> _responceHeadersStorage;

        private MemoryStream _serializerStream;

        public CompressionProcessor()
        {

            _requestHeadersStorage = CompressionInitialHeaders.RequestInitialHeaders;
            _responceHeadersStorage = CompressionInitialHeaders.ResponseInitialHeaders;
            InitCompressor();
            InitDecompressor();
        }

        private void InitCompressor()
        {
            _serializerStream = new MemoryStream(1024);
        }

        private void InitDecompressor()
        {
            _currentOffset = 0;
        }

        private void ModifyTable(string headerName, string headerValue, IndexationType headerType,
                                        List<KeyValuePair<string, string>> useHeadersTable, int index)
        {
                switch (headerType)
                {
                    case IndexationType.Incremental:
                        useHeadersTable.Add(new KeyValuePair<string, string>(headerName, headerValue));
                        break;
                    case IndexationType.Substitution:
                        if (index != -1)
                        {
                            useHeadersTable[index] = new KeyValuePair<string, string>(headerName, headerValue);
                        }
                        else
                        {
                            //If header wasn't found then add it to the table
                            useHeadersTable.Add(new KeyValuePair<string, string>(headerName, headerValue));
                        }
                        break;
                    default:
                        return;
                }
        }

        #region Compression

        private void CompressHeader(Tuple<string, string, IAdditionalHeaderInfo> header,
                                        List<KeyValuePair<string, string>> useHeadersTable)
        {
            byte prefix = 0;
            var headerName = header.Item1;
            var headerValue = header.Item2;
            var headerType = (header.Item3 as Indexation).Type;

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
                    CompressIndexed(new KeyValuePair<string, string>(headerName, headerValue), useHeadersTable);
                    return;
            }

            CompressNonIndexed(headerName, headerValue, headerType, prefix, useHeadersTable);
        }

        private void CompressNonIndexed(string headerName, string headerValue, IndexationType headerType, byte prefix,
                                        List<KeyValuePair<string, string>> useHeadersTable)
        {
            int index = useHeadersTable.GetIndex(kv => kv.Key == headerName);

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

            ModifyTable(headerName, headerValue, headerType, useHeadersTable, index);
        }

        private void CompressIndexed(KeyValuePair<string, string> header, List<KeyValuePair<string, string>> useHeadersTable)
        {
            int index = useHeadersTable.GetIndex(kv => kv.Key == header.Key && kv.Value == header.Value);
            const byte prefix = 7;
            var bytes = index.ToUVarInt(prefix);

            //Set indexed type
            bytes[0] |= (byte) IndexationType.Indexed;

            WriteToOutput(bytes, 0, bytes.Length);
        }

        //Method retypes as many headers as it can to be Indexed
        //and checks if headers marked as indexed are present in the headers table
        private void OptimizeInputAndSendOptimized(List<Tuple<string, string, IAdditionalHeaderInfo> > headers, List<KeyValuePair<string, string>> useHeadersTable)
        {
            for (int i = 0; i < headers.Count; i++ )
            {
                var headerKv = new KeyValuePair<string, string>(headers[i].Item1, headers[i].Item2);
                IndexationType headerType = (headers[i].Item3 as Indexation).Type;

                int index = useHeadersTable.IndexOf(headerKv);

                if (index != -1 && (headerType == IndexationType.Incremental|| headerType == IndexationType.Substitution))
                {
                    CompressIndexed(headerKv, useHeadersTable);
                    headers.Remove(headers[i--]);
                }

                //If header marked as indexed, but not found in the table, compress it as incremental.
                if (index == -1 && headerType == IndexationType.Indexed)
                {
                    CompressNonIndexed(headerKv.Key, headerKv.Value, IndexationType.Incremental, 5, useHeadersTable);
                    headers.Remove(headers[i--]);
                }
            }
        }

        public byte[] Compress(IList<Tuple<string, string, IAdditionalHeaderInfo> > headers, bool isRequest)
        {
            var headersCopy = new List<Tuple<string, string, IAdditionalHeaderInfo>>(headers);
            var useHeadersTable = isRequest ? _requestHeadersStorage : _responceHeadersStorage;
            ClearStream(_serializerStream, (int) _serializerStream.Position);

            OptimizeInputAndSendOptimized(headersCopy, useHeadersTable);

            foreach (var header in headersCopy)
            {
                if (header.Item1 == null || header.Item2 == null || header.Item3 == null)
                {
                    throw new InvalidHeaderException(header);
                }

                CompressHeader(header, useHeadersTable);
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

        private Tuple<string, string, IAdditionalHeaderInfo> ParseHeader(byte[] bytes, List<KeyValuePair<string, string>> useHeadersTable)
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
                    var kv = useHeadersTable[index];
                    return new Tuple<string, string, IAdditionalHeaderInfo>(kv.Key, kv.Value, new Indexation(type));
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
                        name = useHeadersTable[index - 1].Key;
                    }
                    valueLen = bytes[_currentOffset++];
                    value = Encoding.UTF8.GetString(bytes, _currentOffset, valueLen);
                    _currentOffset += valueLen;

                    ModifyTable(name, value, type, useHeadersTable, index - 1);

                    return new Tuple<string, string, IAdditionalHeaderInfo>(name, value, new Indexation(type));
            }

            return default(Tuple<string, string, IAdditionalHeaderInfo>);
        }
        
        private int GetIndex(byte[] bytes, IndexationType type)
        {
            byte prefix = 0;
            byte firstByteValue = (byte) (bytes[_currentOffset] & (~(byte)type));
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
            var type = bytes[_currentOffset];
            if ((type & 0x80) == (byte)IndexationType.Indexed)
            {
                return IndexationType.Indexed;
            }

            if ((type & 0x60) == (byte)IndexationType.WithoutIndexation)
            {
                return IndexationType.WithoutIndexation;
            }

            if ((type & 0x40) == (byte)IndexationType.Incremental)
            {
                return IndexationType.Incremental;
            }

            return IndexationType.Substitution;
        }

        public List<Tuple<string, string, IAdditionalHeaderInfo> > Decompress(byte[] serializedHeaders, bool isRequest)
        {
            var useHeadersTable = isRequest ? _requestHeadersStorage : _responceHeadersStorage;
            var result = new List<Tuple<string, string, IAdditionalHeaderInfo>>(16);
            _currentOffset = 0;

            while (_currentOffset != serializedHeaders.Length)
            {
                var entry = ParseHeader(serializedHeaders, useHeadersTable);
                result.Add(entry);
            }

            return result;
        }

        #endregion

        private void WriteToOutput(byte[] bytes, int offset, int length)
        {
            _serializerStream.WriteAsync(bytes, offset, length);
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
