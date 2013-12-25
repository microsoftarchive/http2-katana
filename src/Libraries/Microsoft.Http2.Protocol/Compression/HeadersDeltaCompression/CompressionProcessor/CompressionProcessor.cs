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
using OpenSSL;

namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    //This headers compression algorithm is described in
    // http://tools.ietf.org/html/draft-ietf-httpbis-header-compression-05
    /// <summary>
    /// This class implement header compression.
    /// </summary>
    internal partial class CompressionProcessor : ICompressionProcessor
    {
        private HeadersList _remoteHeadersTable;
        private HeadersList _remoteRefSet;
        private HeadersList _localHeadersTable;
        private HeadersList _localRefSet;

        private ConnectionEnd _localEnd;

        private HuffmanCompressionProcessor _huffmanProc;

        private bool _isDisposed;

        private MemoryStream _serializerStream;

        private int _maxHeaderByteSize;

        public CompressionProcessor(ConnectionEnd end)
        {
            //default max headers table size
            _maxHeaderByteSize = 4096;

            //05 The header table is initially empty.
            _remoteHeadersTable = new HeadersList();
            _localHeadersTable = new HeadersList();

            //05 The reference set is initially empty.
            _remoteRefSet = new HeadersList();
            _localRefSet = new HeadersList();

            _huffmanProc = new HuffmanCompressionProcessor();

            _localEnd = end;

            InitCompressor();
            InitDecompressor();
        }

        private void MakeHeadersTableBeUpToDate(HeadersList headersTable, HeadersList refTable)
        {
            while (headersTable.StoredHeadersSize >= _maxHeaderByteSize && headersTable.Count > 0)
            {

                var header = headersTable[headersTable.Count - 1];
                headersTable.RemoveAt(headersTable.Count - 1);

                //spec 05
                //3.3.2. Entry Eviction When Header Table Size Changes
                //Whenever an entry is evicted from the header table, any reference to
                //that entry contained by the reference set is removed.

                if (refTable.Contains(header))
                    refTable.Remove(header);
            }
        }

        public void NotifySettingsChanges(int newMaxVal)
        {
            if (newMaxVal <= 0)
                throw new CompressionError(new Exception("incorrect MaxHeadersTable size!"));

            _maxHeaderByteSize = newMaxVal;

            MakeHeadersTableBeUpToDate(_remoteHeadersTable, _remoteRefSet);
            MakeHeadersTableBeUpToDate(_localHeadersTable, _localRefSet);
        }

        private void InitCompressor()
        {
            _serializerStream = new MemoryStream();
        }

        private void InitDecompressor()
        {
            _currentOffset = 0;
        }

        private void InsertToHeadersTable(KeyValuePair<string, string> header, 
                                          HeadersList refSet,
                                          HeadersList headersTable)
        {
            //spec 05
            // The size of an entry is the sum of its name's length in octets (as
            //defined in Section 4.1.2), of its value's length in octets
            //(Section 4.1.2) and of 32 octets.
            int headerLen = header.Key.Length + header.Value.Length + 32;

            //spec 05
            //3.3.3. Entry Eviction when Adding New Entries
            //Whenever a new entry is to be added to the table, any name referenced
            //by the representation of this new entry is cached, and then entries
            //are evicted from the end of the header table until the size of the
            //header table is less than or equal to SETTINGS_HEADER_TABLE_SIZE -
            //new entry size, or until the table is empty.

            //If the size of the new entry is less than or equal to
            //SETTINGS_HEADER_TABLE_SIZE, that entry is added to the table.  It is
            //not an error to attempt to add an entry that is larger than
            //SETTINGS_HEADER_TABLE_SIZE.

            while (headersTable.StoredHeadersSize + headerLen >= _maxHeaderByteSize && headersTable.Count > 0)
            {
                headersTable.RemoveAt(headersTable.Count - 1);

                //spec 05
                //3.3.2. Entry Eviction When Header Table Size Changes
                //Whenever an entry is evicted from the header table, any reference to
                //that entry contained by the reference set is removed.

                if (refSet.Contains(header))
                    refSet.Remove(header);
            }

            //spec 05
            //We should always insert into begin of the headers table. 
            //See 3.2.1.  Header Field Representation Processing
            headersTable.Insert(0, header);
        }
        
        #region Compression

        private byte[] EncodeString(string item, bool useHuffman)
        {
            byte[] itemBts;
            int len = 0;

            const byte prefix = 7;

            byte[] lenBts; //05: String representation | H |  Value Length Prefix (7)  |

            if (!useHuffman)
            {
                itemBts = Encoding.UTF8.GetBytes(item);
                len = item.Length;
                lenBts = len.ToUVarInt(prefix);
            }
            else
            {
                itemBts = Encoding.UTF8.GetBytes(item);
                itemBts = _huffmanProc.Compress(itemBts, _localEnd == ConnectionEnd.Client);

                len = itemBts.Length;
                lenBts = len.ToUVarInt(prefix);

                lenBts[0] |= 0x80; //05: Set huffman to true | 1 |  Value Length Prefix (7)  |
            }

            byte[] result = new byte[lenBts.Length + itemBts.Length];

            Buffer.BlockCopy(lenBts, 0, result, 0, lenBts.Length);
            Buffer.BlockCopy(itemBts, 0, result, lenBts.Length, itemBts.Length);

            return result;
        }

        private void CompressIncremental(KeyValuePair<string, string> header)
        {
            const byte prefix = 6;
            //spec 05
            //05 does not tell anything about case_sensitive | insensitive
            int index = _remoteHeadersTable.FindIndex(kv => kv.Key.Equals(header.Key));
            bool isFound = index != -1;

            /* 05 spec:
            <-- Header  Table -->  <-- Static  Table -->
            +---+-----------+---+  +---+-----------+---+
            | 1 |    ...    | k |  |k+1|    ...    | n |
            +---+-----------+---+  +---+-----------+---+
            ^                   |
            |                   V
            Insertion Point       Drop Point
             */
            if (!isFound)
            {
                index = _staticTable.FindIndex(kv => kv.Key.Equals(header.Value));
                isFound = index != -1;

                if (isFound)
                {
                    index += _remoteHeadersTable.Count;
                }
            }
            //It's necessary to form result array because partial writeToOutput stream can cause problems because of multithreading
            using (var stream = new MemoryStream(64))
            {
                byte[] indexBinary;
                byte[] nameBinary = new byte[0];
                byte[] valueBinary;

                if (isFound)
                {
                    //Header key was found in the header table. Hence we should encode only value
                    indexBinary = (index + 1).ToUVarInt(prefix);
                    valueBinary = EncodeString(header.Value, true);
                }
                else
                {
                    //Header key was not found in the header table. Hence we should encode name and value
                    indexBinary = 0.ToUVarInt(prefix);
                    nameBinary = EncodeString(header.Key, true);
                    valueBinary = EncodeString(header.Value, true);
                }

                //Set without index type
                indexBinary[0] |= (byte)IndexationType.Incremental;

                stream.Write(indexBinary, 0, indexBinary.Length);
                stream.Write(nameBinary, 0, nameBinary.Length);
                stream.Write(valueBinary, 0, valueBinary.Length);

                WriteToOutput(stream.GetBuffer(), 0, (int)stream.Position);
            }

            InsertToHeadersTable(header, _remoteRefSet, _remoteHeadersTable);
        }

        private void CompressWithoutIndexation(KeyValuePair<string, string> header)
        {
            const byte prefix = 6;
            //spec 05
            //05 does not tell anything about case_sensitive | insensitive
            int index = _remoteHeadersTable.FindIndex(kv => kv.Key.Equals(header.Key));
            bool isFound = index != -1;

            /* 05 spec:
            <-- Header  Table -->  <-- Static  Table -->
            +---+-----------+---+  +---+-----------+---+
            | 1 |    ...    | k |  |k+1|    ...    | n |
            +---+-----------+---+  +---+-----------+---+
            ^                   |
            |                   V
            Insertion Point       Drop Point
             */
            if (!isFound)
            {
                index = _staticTable.FindIndex(kv => kv.Key.Equals(header.Value));
                isFound = index != -1;

                if (isFound)
                {
                    index += _remoteHeadersTable.Count;
                }
            }
            //It's necessary to form result array because partial writeToOutput stream can cause problems because of multithreading
            using (var stream = new MemoryStream(64))
            {
                byte[] indexBinary;
                byte[] nameBinary = new byte[0];
                byte[] valueBinary;

                if (isFound)
                {
                    //Header key was found in the header table. Hence we should encode only value
                    indexBinary = (index + 1).ToUVarInt(prefix);
                    valueBinary = EncodeString(header.Value, true); 
                }
                else
                {
                    //Header key was not found in the header table. Hence we should encode name and value
                    indexBinary = 0.ToUVarInt(prefix);
                    nameBinary = EncodeString(header.Key, true);
                    valueBinary = EncodeString(header.Value, true);
                }
                
                //Set without index type
                indexBinary[0] |= (byte)IndexationType.WithoutIndexation;

                stream.Write(indexBinary, 0, indexBinary.Length);
                stream.Write(nameBinary, 0, nameBinary.Length);
                stream.Write(valueBinary, 0, valueBinary.Length);

                WriteToOutput(stream.GetBuffer(), 0, (int)stream.Position);
            }

            InsertToHeadersTable(header, _remoteRefSet, _remoteHeadersTable);
        }

        private void CompressIndexed(KeyValuePair<string, string> header)
        {

            //int index = _remoteRefSet.FindIndex(kv => kv.Key.Equals(header.Key) && kv.Value.Equals(header.Value));
            //bool isFound = index != -1;
            //if (!isFound)
            //{
                //An _indexed representation_ corresponding to an entry _not present_
                //in the reference set entails the following actions:

                //*  The header field corresponding to the referenced entry is
                //emitted.

                //*  The referenced static entry is inserted at the beginning of the
                //header table.

                //*  A reference to this new header table entry is added to the
                //reference set (except if this new entry didn't fit in the
                //header table).

                //spec 05
                //nothing told about case_sensitive | _insensitive comparsion
                int index = _remoteHeadersTable.FindIndex(kv => kv.Key.Equals(header.Key) && kv.Value.Equals(header.Value));
                bool isFound = index != -1;

                /* 05 spec:
                <-- Header  Table -->  <-- Static  Table -->
                +---+-----------+---+  +---+-----------+---+
                | 1 |    ...    | k |  |k+1|    ...    | n |
                +---+-----------+---+  +---+-----------+---+
                ^                   |
                |                   V
                Insertion Point       Drop Point
                 */
                if (!isFound)
                {
                    index = _staticTable.FindIndex(kv => kv.Key.Equals(header.Key) && kv.Value.Equals(header.Value));
                    isFound = index != -1;

                    if (isFound)
                    {
                        index += _remoteHeadersTable.Count;
                        //3.2.1. Header Field Representation Processing
                        //The referenced static entry is inserted at the beginning of the
                        //header table.
                        _remoteHeadersTable.Insert(0, header);
                    }
                }

                if (!isFound)
                {
                    throw new CompressionError(new Exception("cant compress indexed header. Index not found."));
                }

            const byte prefix = 7;
            var bytes = (index + 1).ToUVarInt(prefix);

            //Set indexed type
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

        //spec 05:
        //string prefix is always 7. 
        //See 4.1.2.  String Literal Representation
        private const byte stringPrefix = 7;

        private string DecodeString(byte[] bytes, byte prefix)
        {
            int maxPrefixVal = (1 << prefix) - 1;

            bool isHuffman = (bytes[_currentOffset] & 0x80) != 0; //Get first bit. If true => huffman used

            int len = 0;
            
            //throw away huffman's mask
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
                var decodedBytes = _huffmanProc.Decompress(compressedBytes, _localEnd == ConnectionEnd.Server);
                result = Encoding.UTF8.GetString(decodedBytes);

                _currentOffset += len;

                return result;
            }

            result = Encoding.UTF8.GetString(bytes, _currentOffset, len);
            _currentOffset += len;

            return result;
        }

        //09 -> 8.1.3.4.  Compressing the Cookie Header Field
        private void ProcessCookie(HeadersList toProcess)
        {
            //The Cookie header field [COOKIE] can carry a significant amount of
            //redundant data.

            //The Cookie header field uses a semi-colon (";") to delimit cookie-
            //pairs (or "crumbs").  This header field doesn't follow the list
            //construction rules in HTTP (see [HTTP-p1], Section 3.2.2), which
            //prevents cookie-pairs from being separated into different name-value
            //pairs.  This can significantly reduce compression efficiency as
            //individual cookie-pairs are updated.

            //To allow for better compression efficiency, the Cookie header field
            //MAY be split into separate header fields, each with one or more
            //cookie-pairs.  If there are multiple Cookie header fields after
            //decompression, these MUST be concatenated into a single octet string
            //using the two octet delimiter of 0x3B, 0x20 (the ASCII string "; ").

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
                //Add without last delimeter
                toProcess.Add(new KeyValuePair<string, string>(CommonHeaders.Cookie,
                                                               cookie.ToString(cookie.Length - 2, 2)));
            }
        }

        private Tuple<string, string, IndexationType> ProcessIndexed(int index)
        {
            //An _indexed representation_ with an index value of 0 entails the
            //following actions:
            //o  The reference set is emptied.
            if (index == 0)
            {
                _localRefSet.Clear();
                return default(Tuple<string, string, IndexationType>);
            }

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

            //An _indexed representation_ corresponding to an entry _present_ in
            //the reference set entails the following actions:
            //o  The entry is removed from the reference set.
            if (_localRefSet.Contains(header))
            {
                _localRefSet.Remove(header);
                return null;
            }

            //An _indexed representation_ corresponding to an entry _not present_
            //in the reference set entails the following actions:

            //o  If referencing an element of the static table:

            //*  The header field corresponding to the referenced entry is emitted.

            //*  The referenced static entry is inserted at the beginning of the header table.

            //* A reference to this new header table entry is added to the
            //  reference set (except if this new entry didn't fit in the
            //  header table).

            //o  If referencing an element of the header table:

            //*  The header field corresponding to the referenced entry is emitted.

            //*  The referenced header table entry is added to the reference set.
            if (isInStatic)
            {
                //* StaticTable: The referenced static entry is inserted at the beginning of the header table.
                _localHeadersTable.Insert(0, header);
            }

            //* StaticTable: A reference to this new header table entry is added to the
            //  reference set (except if this new entry didn't fit in the
            //  header table).

            //*  HeadersTable: The referenced header table entry is added to the reference set.
            _localRefSet.Add(header);

            //* Static | Headers table: The header field corresponding to the referenced entry is emitted.
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
                //Index increased by 1 was sent
                name = index < _localHeadersTable.Count ? _localHeadersTable[index - 1].Key : _staticTable[index - 1].Key;
            }

            value = DecodeString(bytes, stringPrefix); 

            //A _literal representation_ that is _not added_ to the header table
            //entails the following action:

            //o  The header field is emitted.
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
                //Index increased by 1 was sent
                name = index - 1 < _localHeadersTable.Count ? 
                                _localHeadersTable[index - 1].Key :
                                _staticTable[index - _localHeadersTable.Count - 1].Key;
            }

            value = DecodeString(bytes, stringPrefix);

            //A _literal representation_ that is _added_ to the header table
            //entails the following actions:

            //o  The header field is emitted.

            //o  The header field is inserted at the beginning of the header table.
            //This action will be performed when ModifyTable will be called

            //o  A reference to the new entry is added to the reference set (except
            //if this new entry didn't fit in the header table).

            var header = new KeyValuePair<string, string>(name, value);

            //o  A reference to the new entry is added to the reference set (except
            //if this new entry didn't fit in the header table).
            _localRefSet.Add(header);

            //o  The header field is inserted at the beginning of the header table.
            //This action will be performed when ModifyTable will be called
            InsertToHeadersTable(header, _localRefSet, _localHeadersTable);

            //o  The header field is emitted.
            return new Tuple<string, string, IndexationType>(name, value, IndexationType.Incremental);
        }

        private Tuple<string, string, IndexationType> ParseHeader(byte[] bytes)
        {
            var type = GetHeaderType(bytes);
            int index = GetIndex(bytes, type);

            switch (type)
            {
                case IndexationType.Indexed:
                    return ProcessIndexed(index);
                case IndexationType.Incremental:
                    return ProcessIncremental(bytes, index);
                case IndexationType.WithoutIndexation:
                    return ProcessWithoutIndexing(bytes, index);
            }

            throw new CompressionError(new Exception("Unknown header type"));
        }
        
        private int GetIndex(byte[] bytes, IndexationType type)
        {
            byte prefix = 0;
            byte firstByteValue = bytes[_currentOffset];

            switch (type)
            {  
                case IndexationType.Incremental:
                case IndexationType.WithoutIndexation:
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
            else if ((typeByte & 0x40) == (byte)IndexationType.WithoutIndexation)
            {
                indexationType = IndexationType.WithoutIndexation;
            }
            else
            {
                indexationType = IndexationType.Incremental;
            }
            //throw type mask away
            bytes[_currentOffset] = (byte)(bytes[_currentOffset] & (~(byte)indexationType));
            return indexationType;
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

                    if (entry == null) //parsed indexed header which was in the refSet already.
                        continue;

                    var header = new KeyValuePair<string, string>(entry.Item1, entry.Item2);
                    
                    if (entry.Item3 == IndexationType.WithoutIndexation)
                    {
                        unindexedHeadersList.Add(header);
                    }
                }

                //TODO Check if this necessary
                /*for (int i = _localRefSet.Count - 1; i >= 0; --i)
                {
                    var header = _localRefSet[i];
                    if (!_localHeadersTable.Contains(header))
                    {
                        _localRefSet.RemoveAll(h => h.Equals(header));
                    }
                }*/

                //Base result on already modified reference set
                var result = new HeadersList(_localRefSet);

                //Add to result Without indexation. They were not added into reference set.
                result.AddRange(unindexedHeadersList);

                ProcessCookie(result);

                //Return result
                return result;
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
            if (_isDisposed)
                return;

            _serializerStream.Dispose();

            _isDisposed = true;
        }
    }
}
