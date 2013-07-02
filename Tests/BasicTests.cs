using System;
using System.Collections;
using System.Collections.Generic;
using SharedProtocol.Compression;
using SharedProtocol.Extensions;
using Org.Mentalis.Security.Ssl;
using SharedProtocol;
using SharedProtocol.ExtendedMath;
using SharedProtocol.Framing;
using Xunit;
using Http2HeadersCompression;

namespace BasicTests
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

        [Fact]
        public void MinExtendedMathWithoutComparerSuccessful()
        {
            var tests = new List<double[]>
                {
                    new [] {1, -2, (double)3, -6},
                    new [] {float.MaxValue, double.MinValue, byte.MaxValue, -6},
                    new [] {int.MaxValue, double.MaxValue, float.MinValue, int.MinValue}
                };

            var results = new[] {-6, double.MinValue, float.MinValue};
  
            for(int i = 0 ; i < tests.Count ; i++)
            {
                Assert.Equal(MathEx.Min(tests[i]), results[i]);
            }
        }

        [Fact]
        public void MinExtendedMathWithComparerSuccessful()
        {
            var tests = new List<string[]>
                {
                    new [] {"abacaba", "me", "helloworld"},
                    new [] {"get", "post", "server"},
                    new [] {"james", "teylor", "euler", "lorentz"}
                };

            var results = new[] { "me", "server", "teylor" };

            for (int i = 0; i < tests.Count; i++)
            {
                Assert.Equal(MathEx.Min(new StringComparer(), tests[i]), results[i]);
            }
        }

        [Fact]
        public void ActiveStreamsSuccessful()
        {
            var session = new Http2Session(null, ConnectionEnd.Client);
            var testCollection = session.ActiveStreams;
            var fm = new FlowControlManager(session);

            testCollection[1] = new Http2Stream(null, 1, Priority.Pri3, null, fm, null);
            testCollection[2] = new Http2Stream(null, 2, Priority.Pri3, null, fm, null);
            testCollection[3] = new Http2Stream(null, 3, Priority.Pri3, null, fm, null);
            testCollection[4] = new Http2Stream(null, 4, Priority.Pri3, null, fm, null);

            fm.DisableStreamFlowControl(testCollection[2]);
            fm.DisableStreamFlowControl(testCollection[4]);

            Assert.Equal(testCollection.NonFlowControlledStreams.Count, 2);
            Assert.Equal(testCollection.FlowControlledStreams.Count, 2);

            bool gotException = false;
            try
            {
                testCollection[4] = new Http2Stream(null, 3, Priority.Pri3, null, fm, null);
            }
            catch (ArgumentException)
            {
                gotException = true;
            }

            Assert.Equal(gotException, true);

            testCollection.Remove(4);

            Assert.Equal(testCollection.Count, 3);
            Assert.Equal(testCollection.ContainsKey(4), false);
        }

        [Fact]
        public void FrameHelperSuccessful()
        {
            const byte input = 1;
            byte result = FrameHelpers.SetBit(input, true, 3);
            Assert.Equal(result, 9);
            result = FrameHelpers.SetBit(result, false, 3);
            Assert.Equal(result, 1);
            result = FrameHelpers.SetBit(result, false, 0);
            Assert.Equal(result, 0);
            result = FrameHelpers.SetBit(result, true, 7);
            Assert.Equal(result, 128);
            result = FrameHelpers.SetBit(result, true, 6);
            Assert.Equal(result, 192);
            result = FrameHelpers.SetBit(result, true, 5);
            Assert.Equal(result, 224);
            result = FrameHelpers.SetBit(result, false, 7);
            Assert.Equal(result, 96);
            result = FrameHelpers.SetBit(result, false, 6);
            Assert.Equal(result, 32);
            result = FrameHelpers.SetBit(result, false, 5);
            Assert.Equal(result, 0);
            result = FrameHelpers.SetBit(result, true, 0);
            Assert.Equal(result, input);
        }

        [Fact]
        public void UVarIntConversionSuccessful()
        {
            var test1337 = 1337.ToUVarInt(5);
            Assert.Equal(1337.FromUVarInt(test1337), 1337);
            test1337 = 1337.ToUVarInt(3);
            Assert.Equal(1337.FromUVarInt(test1337), 1337);
            test1337 = 1337.ToUVarInt(0);
            Assert.Equal(1337.FromUVarInt(test1337), 1337);

            var test0 = 0.ToUVarInt(5);
            Assert.Equal(0.FromUVarInt(test0), 0);
            test0 = 0.ToUVarInt(0);
            Assert.Equal(0.FromUVarInt(test0), 0);

            var test0xfffff = 0xfffff.ToUVarInt(7);
            Assert.Equal(0xfffff.FromUVarInt(test0xfffff), 0xfffff);
            test0xfffff = 0xfffff.ToUVarInt(4);
            Assert.Equal(0xfffff.FromUVarInt(test0xfffff), 0xfffff);
            test0xfffff = 0xfffff.ToUVarInt(0);
            Assert.Equal(0xfffff.FromUVarInt(test0xfffff), 0xfffff);
        }

        [Fact]
        public void CompressionSuccessful()
        {
            var headers = new List<Tuple<string, string, IAdditionalHeaderInfo>>
                {
                    new Tuple<string, string, IAdditionalHeaderInfo>(":method", "get", new Indexation(IndexationType.Indexed)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":path", "test.txt", new Indexation(IndexationType.Substitution)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":version", "http/2.0", new Indexation(IndexationType.Incremental)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":host", "localhost", new Indexation(IndexationType.Substitution)),
                    new Tuple<string, string, IAdditionalHeaderInfo>(":scheme", "https", new Indexation(IndexationType.Substitution)),
                };
            var compressor = new CompressionProcessor();
            var decompressor = new CompressionProcessor();

            byte[] serialized;
            List<Tuple<string, string, IAdditionalHeaderInfo>> decompressed = null;

            for (int i = 0; i < 10; i++)
            {
                serialized = compressor.Compress(headers, false);
                decompressed = decompressor.Decompress(serialized, false);
            }

            for(int i = 0 ; i < headers.Count ; i++)
            {
                Assert.Equal(decompressed.GetValue(headers[i].Item1), headers[i].Item2);
            }
        }

        [Fact]
        public void HeadersCollectionSuccessful()
        {
            var collection = new HeadersCollection
                {
                    new KeyValuePair<string, string>("key1","value1"),
                    new KeyValuePair<string, string>("1key","val1ue"),
                    new KeyValuePair<string, string>("key2","value2"),
                    new KeyValuePair<string, string>("2key", "value2"),
                    new KeyValuePair<string, string>("my-header","my-value")
                };

            int i = 0;
            foreach (var item in collection)
            {
                i++;
            }

            Assert.Equal(i, collection.Count);
            Assert.Equal(new KeyValuePair<string, string>("my-header", "my-value"), collection[0]);
            Assert.Equal(new KeyValuePair<string, string>("key1", "value1"), collection[1]);
            Assert.Equal(new KeyValuePair<string, string>("1key", "val1ue"), collection[2]);
            Assert.Equal(new KeyValuePair<string, string>("key2", "value2"), collection[3]);
            Assert.Equal(new KeyValuePair<string, string>("2key", "value2"), collection[4]);

            collection.SubstituteValue(1, "value2");
            Assert.Equal("value2", collection[1].Value);
            Assert.Equal(collection.IndexOf(new KeyValuePair<string, string>("my-header", "my-value")), 0);
            Assert.Equal(collection.IndexOf(new KeyValuePair<string, string>("key1", "value2")), 1);
            Assert.Equal(collection.IndexOf(new KeyValuePair<string, string>("1key", "val1ue")), 2);
            Assert.Equal(collection.IndexOf(new KeyValuePair<string, string>("key2", "value2")), 3);
            Assert.Equal(collection.IndexOf(new KeyValuePair<string, string>("2key", "value2")), 4);
        }
    }
}
