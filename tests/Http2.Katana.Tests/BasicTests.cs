using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;
using Microsoft.Http2.Protocol.Compression.Huffman;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.Http2Session;
using Microsoft.Http2.Protocol.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace Http2.Katana.Tests
{
    public class BasicTests
    {
        private class StringComparer : IComparer<string>
        {
            int IComparer<string>.Compare(string x, string y)
            {
                return ((new CaseInsensitiveComparer()).Compare(y, x));
            }
        }

        [StandardFact]
        public void ExtendedMathMultipleNumbers()
        {
            var tests = new List<int[]>
                {
                    new [] {1, 0, -1, 10},
                    new [] {16383, 16383, 16383, 16383},
                    new [] {10, int.MaxValue, -100, int.MinValue},
                };

            var results = new[] { -1, 16383, int.MinValue };

            for (int i = 0; i < tests.Count; i++)
            {
                Assert.Equal(MathEx.Min(tests[i]), results[i]);
            }
        }

        [StandardFact]
        public void ExtendedMathComparer()
        {
            var tests = new List<string[]>
                {
                    new[] {"abacaba", "me", "helloworld"},
                    new[] {"get", "post", "server"},
                    new[] {"james", "teylor", "euler", "lorentz"}
                };

            var results = new[] { "me", "server", "teylor" };

            for (int i = 0; i < tests.Count; i++)
            {
                Assert.Equal(MathEx.Min(new StringComparer(), tests[i]), results[i]);
            }
        }

        [StandardFact]
        public void HeadersList()
        {
            var collection = new HeadersList(new[]
                {
                    new KeyValuePair<string, string>("myKey1", "myValue1"), 
                    new KeyValuePair<string, string>("myKey2", "myValue2"),
                    new KeyValuePair<string, string>("myKey3", "myValue3"),
                    new KeyValuePair<string, string>("myKey4", "myValue4"),
                    new KeyValuePair<string, string>("myKey5", "myValue5"),
                    new KeyValuePair<string, string>("myKey6", "myValue6"),
                    new KeyValuePair<string, string>("myKey7", "myValue7"),
                    new KeyValuePair<string, string>("myKey8", "myValue8"),
                    new KeyValuePair<string, string>("myKey9", "myValue9"),
                    new KeyValuePair<string, string>("myKey0", "myValue0")
                });

            Assert.Equal(collection.Count, 10);
            Assert.Equal(collection.StoredHeadersSize, 60 + 80 + 32 * collection.Count);

            collection.Add(new KeyValuePair<string, string>("someAddKey1", "someAddValue1"));
            collection.Add(new KeyValuePair<string, string>("someAddKey2", "someAddValue2"));

            Assert.Equal(collection.Count, 12);
            Assert.Equal(collection.StoredHeadersSize, 60 + 80 + 32 * collection.Count + 22 + 26);

            int headersSize = collection.Sum(header => header.Key.Length + header.Value.Length + 32);

            Assert.Equal(collection.StoredHeadersSize, headersSize);
        }

        [Fact]
        public void HeadersCompression()
        {
            var clientHeaders = new HeadersList
                {
                    new KeyValuePair<string, string>(":method", "get"),
                    new KeyValuePair<string, string>(":path", "/Y3A9NTcuNjE2NjY1fjM5Ljg2NjY2NSZsdmw9NyZzdHk9ciZxPVlhcm9zbGF2bA=="),
                    new KeyValuePair<string, string>(":version", Protocols.Http2),
                    new KeyValuePair<string, string>(":host", "localhost"),
                    new KeyValuePair<string, string>(":scheme", "https"),
                };
            var clientCompressionProc = new CompressionProcessor();
            var serverCompressionProc = new CompressionProcessor();

            var serializedHeaders = clientCompressionProc.Compress(clientHeaders);
            var decompressedHeaders = new HeadersList(serverCompressionProc.Decompress(serializedHeaders));

            foreach (var t in clientHeaders)
            {
                Assert.Equal(decompressedHeaders.GetValue(t.Key), t.Value);
            }

            var serverHeaders = new HeadersList
                {
                    new KeyValuePair<string, string>(":method", "get"),
                    new KeyValuePair<string, string>(":path", "/simpleTest.txt"),
                    new KeyValuePair<string, string>(":version", Protocols.Http2),
                    new KeyValuePair<string, string>(":host", "localhost"),
                    new KeyValuePair<string, string>(":scheme", "https"),
                };

            serializedHeaders = serverCompressionProc.Compress(serverHeaders);
            decompressedHeaders = new HeadersList(clientCompressionProc.Decompress(serializedHeaders));

            foreach (var t in serverHeaders)
            {
                Assert.Equal(decompressedHeaders.GetValue(t.Key), t.Value);
            }

            serverHeaders = new HeadersList
                {
                    new KeyValuePair<string, string>(":status", StatusCode.Code404NotFound.ToString(CultureInfo.InvariantCulture)),
                };

            serializedHeaders = serverCompressionProc.Compress(serverHeaders);
            decompressedHeaders = new HeadersList(clientCompressionProc.Decompress(serializedHeaders));

            foreach (var t in serverHeaders)
            {
                Assert.Equal(decompressedHeaders.GetValue(t.Key), t.Value);
            }
            
            serverHeaders = new HeadersList
                {
                    new KeyValuePair<string, string>("content-type", "text/plain"),
                    new KeyValuePair<string, string>("last-modified", "Wed, 23 Oct 2013 21:32:06 GMT"),
                    new KeyValuePair<string, string>("etag", "1cedo15cb041fc1"),
                    new KeyValuePair<string, string>("content-length", "749761"),
                    new KeyValuePair<string, string>(":status", StatusCode.Code200Ok.ToString(CultureInfo.InvariantCulture)),
                };

            serializedHeaders = serverCompressionProc.Compress(serverHeaders);
            decompressedHeaders = new HeadersList(clientCompressionProc.Decompress(serializedHeaders));

            foreach (var t in serverHeaders)
            {
                Assert.Equal(decompressedHeaders.GetValue(t.Key), t.Value);
            }

            clientHeaders = new HeadersList
                {
                    new KeyValuePair<string, string>(":method", "get"),
                    new KeyValuePair<string, string>(":path", "/index.html"),
                    new KeyValuePair<string, string>(":version", Protocols.Http2),
                    new KeyValuePair<string, string>(":host", "localhost"),
                    new KeyValuePair<string, string>(":scheme", "https"),
                };

            serializedHeaders = clientCompressionProc.Compress(clientHeaders);
            decompressedHeaders = new HeadersList(serverCompressionProc.Decompress(serializedHeaders));

            foreach (var t in clientHeaders)
            {
                Assert.Equal(decompressedHeaders.GetValue(t.Key), t.Value);
            }
        }

        [StandardFact]
        public void BinaryConverter()
        {
            const bool T = true;
            const bool F = false;
            var testInput = new List<bool>(new[] {T, T, T, F, F, F, T, F, T, F, T, F, F});
            var bytes = Microsoft.Http2.Protocol.Compression.Huffman.BinaryConverter.ToBytes(testInput);

            Assert.Equal(bytes[0], 0xe2);
            Assert.Equal(bytes[1], 0xa0);
        }
        
        [StandardFact]
        public void NeverIndexedEmission()
        {
            var serverCompressionProc = new CompressionProcessor();
            var header = new KeyValuePair<string, string>("custom-key", "custom-value");

            byte[] index = { 0x10 };
            byte[] name = Encoding.UTF8.GetBytes(header.Key);
            byte[] nameLength = name.Length.ToUVarInt(7);
            byte[] value = Encoding.UTF8.GetBytes(header.Value);
            byte[] valueLength = value.Length.ToUVarInt(7);

            byte[] encodedHeader = new byte[index.Length + name.Length +
                value.Length + nameLength.Length + valueLength.Length];

            // creates encoded header
            int offset = 0;
            Buffer.BlockCopy(index, 0, encodedHeader, 0, index.Length);
            offset += index.Length;
            Buffer.BlockCopy(nameLength, 0, encodedHeader, offset , nameLength.Length);
            offset += nameLength.Length;
            Buffer.BlockCopy(name, 0, encodedHeader, offset, name.Length);
            offset += name.Length;
            Buffer.BlockCopy(valueLength, 0, encodedHeader, offset, valueLength.Length);
            offset += valueLength.Length;
            Buffer.BlockCopy(value, 0, encodedHeader, offset, value.Length);

            HeadersList deserializedHeaders = serverCompressionProc.Decompress(encodedHeader);

            Assert.Equal(deserializedHeaders[0].Key, header.Key);
            Assert.Equal(deserializedHeaders[0].Value, header.Value);
        }

        [Fact]
        public void HuffmanCompression()
        {
            var compressor = new HuffmanCompressionProcessor();

            const string input =  "cabacabaababbababcacacacaedfghijklmnopqrstuvwxyz"
                                    + "Adsasd131221453!~[]{}{}~~`\'\\!@#$%^&*()_+=90klasdmnvzxcciuhakdkasdfioads"
                                    + "ADBSADLGUCJNZCXNJSLKDGYSADHIASDMNKJLDBOCXBVCXJIMSAD<NSKLDBHCBIUXHCXZNCMSN"
                                    + ",<>?|";

            var inputBytes = Encoding.UTF8.GetBytes(input);

            var now = DateTime.Now.Millisecond;

            var compressed = compressor.Compress(inputBytes);
            var decompressed = compressor.Decompress(compressed);

            var newNow = DateTime.Now.Millisecond;

            var output = Encoding.UTF8.GetString(decompressed);

            Assert.Equal(input, output);

            Console.WriteLine("Compress/decompress time: " + (newNow - now));
            Console.WriteLine("Input length: " + input.Length);
        }

        [StandardFact]
        public void UVarIntConversion()
        {
            var test1337 = 1337.ToUVarInt(5);
            Assert.Equal(Int32Extensions.FromUVarInt(test1337), 1337);
            test1337 = 1337.ToUVarInt(3);
            Assert.Equal(Int32Extensions.FromUVarInt(test1337), 1337);
            test1337 = 1337.ToUVarInt(0);
            Assert.Equal(Int32Extensions.FromUVarInt(test1337), 1337);

            var test0 = 0.ToUVarInt(5);
            Assert.Equal(Int32Extensions.FromUVarInt(test0), 0);
            test0 = 0.ToUVarInt(0);
            Assert.Equal(Int32Extensions.FromUVarInt(test0), 0);

            var test0Xfffff = 0xfffff.ToUVarInt(7);
            Assert.Equal(Int32Extensions.FromUVarInt(test0Xfffff), 0xfffff);
            test0Xfffff = 0xfffff.ToUVarInt(4);
            Assert.Equal(Int32Extensions.FromUVarInt(test0Xfffff), 0xfffff);
            test0Xfffff = 0xfffff.ToUVarInt(0);
            Assert.Equal(Int32Extensions.FromUVarInt(test0Xfffff), 0xfffff);
        }

        [StandardFact]
        public void FrameHelper()
        {
            const byte input = 1;
            byte result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(input, true, 3);
            Assert.Equal(result, 9);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, false, 3);
            Assert.Equal(result, 1);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, false, 0);
            Assert.Equal(result, 0);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, true, 7);
            Assert.Equal(result, 128);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, true, 6);
            Assert.Equal(result, 192);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, true, 5);
            Assert.Equal(result, 224);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, false, 7);
            Assert.Equal(result, 96);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, false, 6);
            Assert.Equal(result, 32);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, false, 5);
            Assert.Equal(result, 0);
            result = Microsoft.Http2.Protocol.Framing.FrameHelper.SetBit(result, true, 0);
            Assert.Equal(result, input);
        }

        [StandardFact]
        public void ExtendedMathWithoutComparer()
        {
            var tests = new List<double[]>
                {
                    new[] {1, -2, (double) 3, -6},
                    new[] {float.MaxValue, double.MinValue, byte.MaxValue, -6},
                    new[] {int.MaxValue, double.MaxValue, float.MinValue, int.MinValue}
                };

            var results = new[] { -6, double.MinValue, float.MinValue };

            for (int i = 0; i < tests.Count; i++)
            {
                Assert.Equal(MathEx.Min(tests[i]), results[i]);
            }
        }

        [StandardFact]
        public void PriorityQueue()
        {
            var itemsCollection = new List<IPriorityItem>
                {
                    new PriorityQueueEntry(null, 0),
                    new PriorityQueueEntry(null, 7),
                    new PriorityQueueEntry(null, 3),
                    new PriorityQueueEntry(null, 4),
                    new PriorityQueueEntry(null, 2),
                    new PriorityQueueEntry(null, 6),
                    new PriorityQueueEntry(null, 2),
                    new PriorityQueueEntry(null, 4),
                    new PriorityQueueEntry(null, 1),
                    new PriorityQueueEntry(null, 6),
                    new PriorityQueueEntry(null, 0),
                };

            var queue = new PriorityQueue(itemsCollection);
            Assert.Equal(queue.Count, 11);
            var firstItem1 = queue.First();
            Assert.Equal(((PriorityQueueEntry)firstItem1).Priority, 7);
            var lastItem1 = queue.Last();
            Assert.Equal(((PriorityQueueEntry)lastItem1).Priority, 0);
            var peekedItem1 = queue.Peek();
            Assert.Equal(((PriorityQueueEntry)peekedItem1).Priority, 7);
            var item1 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item1).Priority, 7);
            var item2 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item2).Priority, 6);
            var item3 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item3).Priority, 6);
            var item4 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item4).Priority, 4);
            var item5 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item5).Priority, 4);
            var item6 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item6).Priority, 3);
            var item7 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item7).Priority, 2);
            var item8 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item8).Priority, 2);
            var item9 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item9).Priority, 1);
            var item10 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item10).Priority,0);
            var item11 = queue.Dequeue();
            Assert.Equal(((PriorityQueueEntry)item11).Priority, 0);
        }

        [StandardFact]
        public void ConcatAndSplitMultipleHeaders()
        {
            var headers = new HeadersList()
                {
                    new KeyValuePair<string, string>(":scheme", "https"),
                    new KeyValuePair<string, string>(":scheme", "http"),
                    new KeyValuePair<string, string>("another-custom-key", "custom-value"),

                    // this headers must match after decompression
                    new KeyValuePair<string, string>("custom-key", "custom-value"),
                    new KeyValuePair<string, string>("custom-key", "another-custom-value"),
                    new KeyValuePair<string, string>("custom-key", "custom-value"),
                };

            var concatenatingHeaders = new HeadersList(headers);
            Http2Session.ConcatMultipleHeaders(concatenatingHeaders);

            var compressionProc = new CompressionProcessor();
            var compressedHeaders = compressionProc.Compress(concatenatingHeaders);
            var decompressedHeaders = compressionProc.Decompress(compressedHeaders);

            Http2Session.SplitMultipleHeaders(decompressedHeaders);

            for (int i = 0; i < headers.Count; i++)
            {
                Assert.Equal(headers[i], decompressedHeaders[i]);
            }

            Assert.Equal(headers.Count, decompressedHeaders.Count);
        }
    }
}