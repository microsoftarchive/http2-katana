// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Http2.Protocol.Exceptions;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.Compression.Huffman;

namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    // This headers compression algorithm is described in
    // http://tools.ietf.org/html/draft-ietf-httpbis-header-compression-07
    /// <summary>
    /// This class implement header compression.
    /// </summary>
    internal partial class CompressionProcessor : ICompressionProcessor
    {
        /* 08 -> 3.1
        When used for bidirectional communication, such as in HTTP, the
        encoding and decoding contexts maintained by an endpoint are
        completely independent. */
        private HeadersList _remoteHeadersTable;
        private HeadersList _remoteRefSet;
        private HeadersList _localHeadersTable;
        private HeadersList _localRefSet;

        private HuffmanCompressionProcessor _huffmanProc;

        private bool _isDisposed;

        private MemoryStream _serializerStream;

        private int _maxHeaderByteSize;
        private const int MaxStringLength = 255;

        /* 08 -> 7.3
        This new maximum size MUST be lower than or equal to the value 
        of the setting SETTINGS_HEADER_TABLE_SIZE */
        private int _settingsHeaderTableSize;
        private bool _wasSettingHeaderTableSizeReceived;

        // This flag is set after the applying SETTINGS_HEADER_TABLE_SIZE 
        // and is reset the first calling Compress().
        private bool _needToSendEncodingContextUpdate;

        public CompressionProcessor()
        {
            /* 13 -> 6.5.2
            The initial value is 4,096 bytes. */
            _maxHeaderByteSize = 4096;
            _wasSettingHeaderTableSizeReceived = false;

            /* 08 -> 3.2
            The header table is initially empty. */
            _remoteHeadersTable = new HeadersList();
            _localHeadersTable = new HeadersList();

            /* 08 -> 3.3
            The reference set is initially empty. */
            _remoteRefSet = new HeadersList();
            _localRefSet = new HeadersList();

            _huffmanProc = new HuffmanCompressionProcessor();

            InitCompressor();
            InitDecompressor();
        }

        private void EvictHeaderTableEntries(HeadersList headersTable, HeadersList refTable)
        {
            /* 08 -> 5.2
            Whenever the maximum size for the header table is made smaller,
            entries are evicted from the end of the header table until the size
            of the header table is less than or equal to the maximum size. */
            while (headersTable.StoredHeadersSize >= _maxHeaderByteSize && headersTable.Count > 0)
            {
                var header = headersTable[headersTable.Count - 1];
                headersTable.RemoveAt(headersTable.Count - 1);

                /* 08 -> 5.2
                Whenever an entry is evicted from the header table, any reference to
                that entry contained by the reference set is removed. */
                if (refTable.Contains(header))
                    refTable.Remove(header);
            }
        }

        public void NotifySettingsChanges(int newMaxVal)
        {
            if (newMaxVal <= 0)
                throw new CompressionError("invalid max header table size in settings");

            _wasSettingHeaderTableSizeReceived = true;
            _settingsHeaderTableSize = newMaxVal;

            _maxHeaderByteSize = newMaxVal;

            EvictHeaderTableEntries(_remoteHeadersTable, _remoteRefSet);
            EvictHeaderTableEntries(_localHeadersTable, _localRefSet);

            _needToSendEncodingContextUpdate = true;
        }

        /// <summary>
        /// Change Max Header Table Size when receiving appropriate Encoding Context Update
        /// </summary>
        /// <param name="newMaxVal"></param>
        private void ChangeMaxHeaderTableSize(int newMaxVal)
        {
            if (newMaxVal <= 0)
                throw new CompressionError("invalid max header table size");

            _maxHeaderByteSize = newMaxVal;

            EvictHeaderTableEntries(_remoteHeadersTable, _remoteRefSet);
            EvictHeaderTableEntries(_localHeadersTable, _localRefSet);
        }

        private void WriteEncodingContextUpdate(byte[] buffer)
        {
            WriteToOutput(buffer, 0, buffer.Length);
            _needToSendEncodingContextUpdate = false;
        }

        private void InitCompressor()
        {
            _serializerStream = new MemoryStream();
        }

        private void InitDecompressor()
        {
            _currentOffset = 0;
        }

        private void InsertToHeadersTable(KeyValuePair<string, string> header, HeadersList refSet,
            HeadersList headersTable)
        {
            /* 08 -> 5.1
            The size of an entry is the sum of its name's length in octets (as
            defined in Section 4.1.2), of its value's length in octets
            (Section 4.1.2) and of 32 octets. */
            int headerLen = header.Key.Length + header.Value.Length + 32;

            /* 08 -> 5.3
            Whenever a new entry is to be added to the table, any name referenced
            by the representation of this new entry is cached, and then entries
            are evicted from the end of the header table until the size of the
            header table is less than or equal to (maximum size - new entry
            size), or until the table is empty. 
            
            If the size of the new entry is less than or equal to the maximum
            size, that entry is added to the table.  It is not an error to
            attempt to add an entry that is larger than the maximum size. */

            while (headersTable.StoredHeadersSize + headerLen >= _maxHeaderByteSize && headersTable.Count > 0)
            {
                headersTable.RemoveAt(headersTable.Count - 1);

                /* 08 -> 5.2
                Whenever an entry is evicted from the header table, any reference to
                that entry contained by the reference set is removed. */

                if (refSet.Contains(header))
                    refSet.Remove(header);
            }

            /* 08 -> 3.2.1
            We should always insert into 
            begin of the headers table. */
            headersTable.Insert(0, header);
        }
        
        #region Compression

        private byte[] EncodeString(string item, bool useHuffman)
        {
            byte[] itemBts;
            int len;

            const byte prefix = 7;

            byte[] lenBts;

            if (!useHuffman)
            {
                itemBts = Encoding.UTF8.GetBytes(item);
                len = item.Length;
                lenBts = len.ToUVarInt(prefix);
            }
            else
            {
                itemBts = Encoding.UTF8.GetBytes(item);
                itemBts = _huffmanProc.Compress(itemBts);

                len = itemBts.Length;
                lenBts = len.ToUVarInt(prefix);

                lenBts[0] |= 0x80;
            }

            byte[] result = new byte[lenBts.Length + itemBts.Length];

            Buffer.BlockCopy(lenBts, 0, result, 0, lenBts.Length);
            Buffer.BlockCopy(itemBts, 0, result, lenBts.Length, itemBts.Length);

            return result;
        }

        private void CompressIncremental(KeyValuePair<string, string> header)
        {
            const byte prefix = (byte)UVarIntPrefix.Incremental;
            int index = _remoteHeadersTable.FindIndex(kv => kv.Key.Equals(header.Key, StringComparison.OrdinalIgnoreCase));
            bool isFound = index != -1;

            // It's necessary to form result array because partial writeToOutput stream
            // can cause problems because of multithreading
            using (var stream = new MemoryStream(64))
            {
                byte[] indexBinary;
                byte[] nameBinary = new byte[0];
                byte[] valueBinary;

                if (isFound)
                {
                    // Header key was found in the header table. Hence we should encode only value
                    indexBinary = (index + 1).ToUVarInt(prefix);
                    valueBinary = EncodeString(header.Value, true);
                }
                else
                {
                    // Header key was not found in the header table. Hence we should encode name and value
                    indexBinary = 0.ToUVarInt(prefix);
                    nameBinary = EncodeString(header.Key, true);
                    valueBinary = EncodeString(header.Value, true);
                }

                // Set without index type
                indexBinary[0] |= (byte)IndexationType.Incremental;

                stream.Write(indexBinary, 0, indexBinary.Length);
                stream.Write(nameBinary, 0, nameBinary.Length);
                stream.Write(valueBinary, 0, valueBinary.Length);

                WriteToOutput(stream.GetBuffer(), 0, (int)stream.Position);
            }

            InsertToHeadersTable(header, _remoteRefSet, _remoteHeadersTable);
        }

        private void CompressIndexed(KeyValuePair<string, string> header)
        {
            /* 08 -> 4.1
            An _indexed representation_ corresponding to an entry _not present_
            in the reference set entails the following actions:

            o  If referencing an element of the static table:

                *  The header field corresponding to the referenced entry is
                    emitted.

                *  The referenced static entry is inserted at the beginning of the
                    header table.

                *  A reference to this new header table entry is added to the
                    reference set, except if this new entry didn't fit in the
                    header table. */

            int index = _remoteHeadersTable.FindIndex(kv => kv.Key.Equals(header.Key) && kv.Value.Equals(header.Value));
            bool isFound = index != -1;

            if (!isFound)
            {
                index = _staticTable.FindIndex(kv => kv.Key.Equals(header.Key,StringComparison.OrdinalIgnoreCase) 
                                                    && kv.Value.Equals(header.Value, StringComparison.OrdinalIgnoreCase));
                isFound = index != -1;

                if (isFound)
                {
                    index += _remoteHeadersTable.Count;
                    /* 08 -> 4.1
                    The referenced static entry is inserted at the beginning of the
                    header table. */
                    _remoteHeadersTable.Insert(0, header);
                }
            }

            if (!isFound)
            {
                throw new CompressionError("cant compress indexed header. Index not found.");
            }

            const byte prefix = (byte)UVarIntPrefix.Indexed;
            var bytes = (index + 1).ToUVarInt(prefix);

            // Set indexed type
            bytes[0] |= (byte)IndexationType.Indexed;

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
                    // Not there, Will send
                    toSend.Add(header);
                }
                else
                {
                    // Already there, don't delete
                    toDelete.Remove(header);
                }
            }

            /* 08 -> 5.1
            After applying an updated value of the HTTP/2 setting
            SETTINGS_HEADER_TABLE_SIZE that changes the maximum size of the
            header table used by the encoder, the encoder MUST signal this change
            via an encoding context update (see Section 7.3).  This encoding
            context update MUST occur at the beginning of the first header block
            following the SETTINGS frame sent to acknowledge the application of
            the updated settings.
            */
            if (_needToSendEncodingContextUpdate)
            {
                WriteEncodingContextUpdate(EncodingContextHelper.GetUpdateBytes(_settingsHeaderTableSize));
            }

            foreach (var header in toDelete)
            {
                // Anything left in toDelete, should send, so it is deleted from ref set.
                CompressIndexed(header);
                _remoteRefSet.Remove(header); // Update our copy
            }

            foreach (var header in toSend)
            {
                // Send whatever was left in headersCopy
                if (_remoteHeadersTable.Contains(header) || _staticTable.Contains(header))
                {
                    CompressIndexed(header);
                }
                else
                {
                    CompressIncremental(header);
                }

                _remoteRefSet.Add(header);
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

        /* 08 -> 6.2
        String Length:  The number of octets used to encode the string
        literal, encoded as an integer with 7-bit prefix. */
        private const byte stringPrefix = 7;

        private string DecodeString(byte[] bytes, byte prefix)
        {
            int maxPrefixVal = (1 << prefix) - 1;

            // Get first bit. If true => huffman used
            bool isHuffman = (bytes[_currentOffset] & 0x80) != 0; 

            int len;
            
            // throw away huffman's mask
            bytes[_currentOffset] &= 0x7f;

            if ((bytes[_currentOffset]) < maxPrefixVal)
            {
                len = bytes[_currentOffset++]; 
            }
            else
            {
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

                len = Int32Extensions.FromUVarInt(numberBytes);
            }

            string result = String.Empty;

            if (isHuffman)
            {
                var compressedBytes = new byte[len];
                Buffer.BlockCopy(bytes, _currentOffset, compressedBytes, 0, len);
                var decodedBytes = _huffmanProc.Decompress(compressedBytes);
                result = Encoding.UTF8.GetString(decodedBytes);

                _currentOffset += len;
            }
            else
            {
                result = Encoding.UTF8.GetString(bytes, _currentOffset, len);
                _currentOffset += len;
            }

            /* 08 -> 8.4
            An implementation has to set a limit for the values it accepts for
            integers, as well as for the encoded length. In the same way, it has
            to set a limit to the length it accepts for string literals. */
            if (result.Length > MaxStringLength)
                throw new CompressionError("Header name or value is too large");

            return result;
        }
    
        private void ProcessCookie(HeadersList toProcess)
        {
            /* 13 -> 8.1.2.4
            If there are multiple Cookie header fields after
            decompression, these MUST be concatenated into a single octet string
            using the two octet delimiter of 0x3B, 0x20 (the ASCII string "; "). */

            const string delimiter = "; ";
            var cookie = new StringBuilder(String.Empty);

            for (int i = 0; i < toProcess.Count; i++)
            {
                if (!toProcess[i].Key.Equals(CommonHeaders.Cookie))
                    continue;

                cookie.Append(toProcess[i].Value);
                cookie.Append(delimiter);
                toProcess.RemoveAt(i--);
            }

            if (cookie.Length > 0)
            {
                // Add without last delimeter
                toProcess.Add(new KeyValuePair<string, string>(CommonHeaders.Cookie,
                                                               cookie.ToString(cookie.Length - 2, 2)));
            }
        }

        private Tuple<string, string, IndexationType> ProcessIndexed(int index)
        {
            /* 08 -> 7.1
            The index value of 0 is not used. It MUST be treated as a decoding
            error if found in an indexed header field representation. */
            if (index == 0)
                throw new CompressionError("indexed representation with zero value");

            var header = default(KeyValuePair<string, string>);
            bool isInStatic = index > _localHeadersTable.Count & index <= _localHeadersTable.Count + _staticTable.Count;
            bool isInHeaders = index <= _localHeadersTable.Count;

            if (isInStatic)
            {
                header = _staticTable[index - _localHeadersTable.Count - 1];
            }
            else if (isInHeaders)
            {
                header = _localHeadersTable[index - 1];           
            }
            else
            {
                throw new IndexOutOfRangeException("no such index nor in static neither in headers tables");
            }

            /* 08 -> 4.1
            An _indexed representation_ corresponding to an entry _present_ in
            the reference set entails the following actions:

            o  The entry is removed from the reference set. */
            if (_localRefSet.Contains(header))
            {
                _localRefSet.Remove(header);
                return null;
            }

            /* 08 -> 4.1
            An _indexed representation_ corresponding to an entry _not present_
            in the reference set entails the following actions:

            o  If referencing an element of the static table:

                *  The header field corresponding to the referenced entry is
                    emitted.

                *  The referenced static entry is inserted at the beginning of the
                    header table.

                *  A reference to this new header table entry is added to the
                    reference set, except if this new entry didn't fit in the
                    header table.

            o  If referencing an element of the header table:

                *  The header field corresponding to the referenced entry is
                    emitted.

                *  The referenced header table entry is added to the reference
                    set.
            */
            if (isInStatic)
            {
                _localHeadersTable.Insert(0, header);
            }

            _localRefSet.Add(header);

            return new Tuple<string, string, IndexationType>(header.Key, header.Value, IndexationType.Indexed);
        }

        private Tuple<string, string, IndexationType> ProcessWithoutIndexing(byte[] bytes, int index)
        {
            string name;
            string value;

            if (index == 0)
            {
                name = DecodeString(bytes, stringPrefix); 
            }
            else
            {
                // Index increased by 1 was sent
                name = index < _localHeadersTable.Count ? _localHeadersTable[index - 1].Key : _staticTable[index - 1].Key;
            }

            value = DecodeString(bytes, stringPrefix);

            /* 08 -> 4.1
            A _literal representation_ that is _not added_ to the header table
            entails the following action:

            o  The header field is emitted. */
            return new Tuple<string, string, IndexationType>(name, value, IndexationType.WithoutIndexation);
        }

        private Tuple<string, string, IndexationType> ProcessIncremental(byte[] bytes, int index)
        {
            string name;
            string value;
            
            if (index == 0)
            {
                name = DecodeString(bytes, stringPrefix); 
            }
            else
            {
                // Index increased by 1 was sent
                name = index - 1 < _localHeadersTable.Count ? 
                                _localHeadersTable[index - 1].Key :
                                _staticTable[index - _localHeadersTable.Count - 1].Key;
            }

            value = DecodeString(bytes, stringPrefix);

            /* 08 -> 4.1
            A _literal representation_ that is _added_ to the header table
            entails the following actions:

            o  The header field is emitted.

            o  The header field is inserted at the beginning of the header table.

            o  A reference to the new entry is added to the reference set (except
                if this new entry didn't fit in the header table). */

            // o  The header field is inserted at the beginning of the header table.
            // This action will be performed when ModifyTable will be called

            var header = new KeyValuePair<string, string>(name, value);

            _localRefSet.Add(header);

            //This action will be performed when ModifyTable will be called
            InsertToHeadersTable(header, _localRefSet, _localHeadersTable);

            return new Tuple<string, string, IndexationType>(name, value, IndexationType.Incremental);
        }

        private Tuple<string, string, IndexationType> ProcessEncodingContextUpdate(int index, bool clearReferenceSet)
        {
            if (!clearReferenceSet)
            {
                /* 08 -> 7.3
                This new maximum size MUST be lower than
                or equal to the value of the setting SETTINGS_HEADER_TABLE_SIZE */                
                int newTableSize = index;
                 
                if (_wasSettingHeaderTableSizeReceived && (newTableSize <= _settingsHeaderTableSize))
                {
                    ChangeMaxHeaderTableSize(newTableSize);
                }
                else if (!_wasSettingHeaderTableSizeReceived)
                {
                    ChangeMaxHeaderTableSize(newTableSize);
                }
                else
                {
                    throw new CompressionError("incorrect max header table size in Encoding Context Update");
                }
            }
            else if (index == 0)
            {
                _localRefSet.Clear();                
            }
            else
            {
                throw new CompressionError("incorrect format of Encoding Context Update");
            }

            return null;
        }

        private Tuple<string, string, IndexationType> ProcessNeverIndexed(byte [] bytes, int index)
        {
            /* 08 -> 7.2.2
            The encoding of the representation is the same as for the literal
            header field without indexing representation. */

            string name;
            string value;

            if (index == 0)
            {
                name = DecodeString(bytes, stringPrefix);
            }
            else
            {
                // Index increased by 1 was sent
                name = index < _localHeadersTable.Count ? _localHeadersTable[index - 1].Key : _staticTable[index - 1].Key;
            }

            value = DecodeString(bytes, stringPrefix);

            return new Tuple<string, string, IndexationType>(name, value, IndexationType.NeverIndexed);
        }

        private Tuple<string, string, IndexationType> ParseHeader(byte[] bytes)
        {
            var type = GetHeaderType(bytes);

            /* 08 -> 7.3
            The flag bit being set to '1' signals that the reference set is
            emptied. The flag bit being set to '0' signals that a change to the maximum
            size of the header table. */
            bool clearReferenceSet = false;
            if (type == IndexationType.EncodingContextUpdate)
                clearReferenceSet = GetClearFlag(bytes);

            int index = GetIndex(bytes, type);

            try
            {
                switch (type)
                {
                    case IndexationType.Indexed:
                        return ProcessIndexed(index);
                    case IndexationType.Incremental:
                        return ProcessIncremental(bytes, index);
                    case IndexationType.EncodingContextUpdate:
                        return ProcessEncodingContextUpdate(index, clearReferenceSet);
                    case IndexationType.NeverIndexed:
                        return ProcessNeverIndexed(bytes, index);
                    case IndexationType.WithoutIndexation:
                        return ProcessWithoutIndexing(bytes, index);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new CompressionError(ex.Message);
            }
            
            throw new CompressionError("Unknown indexation type");
        }
        
        private int GetIndex(byte[] bytes, IndexationType type)
        {
            byte prefix = 0;
            byte firstByteValue = bytes[_currentOffset];

            switch (type)
            {
                case IndexationType.Incremental:
                    prefix = (byte)UVarIntPrefix.Incremental;
                    break;
                case IndexationType.WithoutIndexation:
                    prefix = (byte)UVarIntPrefix.WithoutIndexing;
                    break;
                case IndexationType.Indexed:
                    prefix = (byte)UVarIntPrefix.Indexed;
                    break;
                case IndexationType.EncodingContextUpdate:
                    prefix = (byte)UVarIntPrefix.EncodingContextUpdate;
                    break;
                case IndexationType.NeverIndexed:
                    prefix = (byte) UVarIntPrefix.NeverIndexed;
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
                if ((bytes[_currentOffset + i] & (byte)IndexationType.Indexed) == 0)
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

            if ((typeByte & (byte)IndexationType.Indexed) == (byte)IndexationType.Indexed)
            {
                indexationType = IndexationType.Indexed;
            }
            else if ((typeByte & (byte)IndexationType.Incremental) == (byte)IndexationType.Incremental)
            {
                indexationType = IndexationType.Incremental;
            }
            else if ((typeByte & (byte)IndexationType.EncodingContextUpdate) == 
                (byte)IndexationType.EncodingContextUpdate)
            {
                indexationType = IndexationType.EncodingContextUpdate;
            }
            else if ((typeByte & (byte) IndexationType.NeverIndexed) ==
                     (byte) IndexationType.NeverIndexed)
            {
                indexationType = IndexationType.NeverIndexed;
            }
            /* When we get the type, WithoutIndexation type is assigned when other types are not suitable. 
            Therefore mask is not used since pattern of any representation is suitable to 0x00 mask. */
            else
            {
                indexationType = IndexationType.WithoutIndexation;
            }
            // throw type mask away
            bytes[_currentOffset] = (byte)(bytes[_currentOffset] & (~(byte)indexationType));
            return indexationType;
        }

        /* 08 -> 7.3
        An encoding context update starts with the '001' 3-bit pattern.        
        It is followed by a flag specifying the type of the change, and by
        any data necessary to describe the change itself. */
        private bool GetClearFlag(byte[] bytes)
        {
            const byte mask = 0x10; // depends on the pattern length
            bool flag = (bytes[_currentOffset] & mask) == mask;
            bytes[_currentOffset] = (byte)(bytes[_currentOffset] & (~mask));
            return flag;
        }

        public HeadersList Decompress(byte[] serializedHeaders)
        {
            try
            {
                _currentOffset = 0;
                var unindexedHeadersList = new HeadersList();

                while (_currentOffset != serializedHeaders.Length)
                {
                    var entry = ParseHeader(serializedHeaders);

                    // parsed indexed header which was already in the refSet 
                    if (entry == null) 
                        continue;

                    var header = new KeyValuePair<string, string>(entry.Item1, entry.Item2);
                    
                    if (entry.Item3 == IndexationType.WithoutIndexation ||
                        entry.Item3 == IndexationType.NeverIndexed)
                    {
                        unindexedHeadersList.Add(header);
                    }
                }

                // Base result on already modified reference set
                var result = new HeadersList(_localRefSet);

                // Add to result Without indexation and Never Indexed
                // They were not added into reference set
                result.AddRange(unindexedHeadersList);

                ProcessCookie(result);

                return result;
            }
            catch (Exception e)
            {
                throw new CompressionError(e.Message);
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
            if (_isDisposed)
                return;

            _serializerStream.Dispose();

            _isDisposed = true;
        }
    }
}
