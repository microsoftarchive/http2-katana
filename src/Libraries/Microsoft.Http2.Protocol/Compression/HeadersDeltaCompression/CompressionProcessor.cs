using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Org.Mentalis.Security.Ssl;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.Extensions;

namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    //This headers compression algorithm is described in
    // http://tools.ietf.org/html/draft-ietf-httpbis-header-compression-01
    /// <summary>
    /// This class implement header compression.
    /// </summary>
    internal class CompressionProcessor : ICompressionProcessor
    {
        private const int MaxHeaderByteSize = 4096;

        private readonly HeadersList _localHeaderTable;
        private readonly HeadersList _remoteHeaderTable;
        private HeadersList _localRefSet;
        private readonly HeadersList _remoteRefSet;

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
            _localRefSet = new HeadersList();
            _remoteRefSet = new HeadersList();

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

        /// <summary>
        /// Modifies the table.
        /// </summary>
        /// <param name="headerName">Name of the header.</param>
        /// <param name="headerValue">The header value.</param>
        /// <param name="headerType">Type of the header.</param>
        /// <param name="useHeadersTable">The use headers table.</param>
        /// <param name="index">The index.</param>
        private static void ModifyTable(string headerName, string headerValue, IndexationType headerType,
                                        HeadersList useHeadersTable, int index)
        {
            int headerLen = headerName.Length + headerValue.Length + sizeof(Int32);

            //spec 04: 3.2.3.  Header Table Management
            //   The header table can be modified by either adding a new entry to it
            //or by replacing an existing one.  Before doing such a modification,
            //it has to be ensured that the header table size will stay lower than
            //or equal to the SETTINGS_MAX_BUFFER_SIZE limit (see Section 5).  To
            //achieve this, repeatedly, the first entry of the header table is
            //removed, until enough space is available for the modification.
               
            //A consequence of removing one or more entries at the beginning of the
            //header table is that the remaining entries are renumbered.  The first
            //entry of the header table is always associated to the index 0.

            //When the modification of the header table is the replacement of an
            //existing entry, the replaced entry is the one indicated in the
            //literal representation before any entry is removed from the header
            //table.  If the entry to be replaced is removed from the header table
            //when performing the size adjustment, the replacement entry is
            //inserted at the beginning of the header table.

            //The addition of a new entry with a size greater than the
            //SETTINGS_MAX_BUFFER_SIZE limit causes all the entries from the header
            //table to be dropped and the new entry not to be added to the header
            //table.  The replacement of an existing entry with a new entry with a
            //size greater than the SETTINGS_MAX_BUFFER_SIZE has the same
            //consequences.



            switch (headerType)
            {
                case IndexationType.Incremental:
                    while (useHeadersTable.StoredHeadersSize + headerLen > MaxHeaderByteSize && useHeadersTable.Count > 0)
                    {
                            useHeadersTable.RemoveAt(0);
                    }
                    useHeadersTable.Add(new KeyValuePair<string, string>(headerName, headerValue));
                    break;
                case IndexationType.Substitution:
                    if (index != -1)
                    {
                        var header = useHeadersTable[index];
                        int substHeaderLen = header.Key.Length + header.Value.Length + sizeof(Int32);
                        int deletedHeadersCounter = 0;

                        while (useHeadersTable.StoredHeadersSize + headerLen - substHeaderLen > MaxHeaderByteSize)
                        {
                            if (useHeadersTable.Count > 0)
                            {
                                useHeadersTable.RemoveAt(0);
                                deletedHeadersCounter++;
                            }
                        }

                        if (index >= deletedHeadersCounter)
                        {
                            useHeadersTable[index - deletedHeadersCounter] = 
                                            new KeyValuePair<string, string>(headerName, headerValue);
                        }
                        else
                        {
                            useHeadersTable.Insert(0, new KeyValuePair<string, string>(headerName, headerValue));
                        }
                    }
                    //If header wasn't found then add it to the table
                    else
                    {
                        while (useHeadersTable.StoredHeadersSize + headerLen > MaxHeaderByteSize && useHeadersTable.Count > 0)
                        {
                            useHeadersTable.RemoveAt(0);
                        }
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
            //spec 04 
            //A header name and an entry name match if they are equal
            //using a character-based, _case insensitive_ comparison (the case
            //insensitive comparison is used because HTTP header names are defined
            //in a case insensitive way).  A header value and an entry value match
            //if they are equal using a character-based, _case sensitive_
            //comparison.
            int index = _remoteHeaderTable.FindIndex(kv => kv.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase));

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
            //spec 04 
            //A header name and an entry name match if they are equal
            //using a character-based, _case insensitive_ comparison (the case
            //insensitive comparison is used because HTTP header names are defined
            //in a case insensitive way).  A header value and an entry value match
            //if they are equal using a character-based, _case sensitive_
            //comparison.
            int index = _remoteHeaderTable.FindIndex(kv => kv.Key.Equals(header.Key, StringComparison.OrdinalIgnoreCase)
                                                           && kv.Value.Equals(header.Value, StringComparison.Ordinal));
            const byte prefix = 7;
            var bytes = index.ToUVarInt(prefix);

            //Set indexed type
            bytes[0] |= (byte) IndexationType.Indexed;

            WriteToOutput(bytes, 0, bytes.Length);
        }

        public byte[] Compress(HeadersList headers)
        {
            var toSend = new HeadersList();
            var toDelete = new HeadersList(_remoteRefSet);
            ClearStream(_serializerStream, (int) _serializerStream.Position);

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
                    int replacedIndex = index;
                    if (type == IndexationType.Substitution)
                    {
                        replacedIndex = GetIndex(bytes, type);
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

                    ModifyTable(name, value, type, _localHeaderTable, replacedIndex);

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

            return Int32Extensions.FromUVarInt(numberBytes);
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
                var workingSet = new HeadersList(_localRefSet);
                var unindexedHeadersList = new HeadersList();
                _currentOffset = 0;

                while (_currentOffset != serializedHeaders.Length)
                {
                    var entry = ParseHeader(serializedHeaders);
                    var header = new KeyValuePair<string, string>(entry.Item1, entry.Item2);

                    if (entry.Item3 == IndexationType.Indexed)
                    {
                        if (workingSet.Contains(header))
                            workingSet.RemoveAll(h => h.Equals(header));
                        else
                            workingSet.Add(header);
                    }
                    else if (entry.Item3 == IndexationType.WithoutIndexation)
                    {
                        unindexedHeadersList.Add(header);
                    }
                    else
                    {
                        workingSet.Add(header);
                    }
                }

                _localRefSet = new HeadersList(workingSet);

                for (int i = _localRefSet.Count - 1; i >= 0; --i)
                {
                    var header = _localRefSet[i];
                    if (!_localHeaderTable.Contains(header))
                        _localRefSet.RemoveAll(h => h.Equals(header));
                }

                workingSet.AddRange(unindexedHeadersList);
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

        private static void ClearStream(Stream input, int len)
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
