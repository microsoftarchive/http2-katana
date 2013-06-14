using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Org.Mentalis.Security.Ssl;
using SharedProtocol;
using SharedProtocol.Compression;
using SharedProtocol.ExtendedMath;
using SharedProtocol.Framing;
using Xunit;
using System.Configuration;

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
        public void CompressionSuccessful()
        {
            var proc = new CompressionProcessor();

            const string helloWorld = "Hello World";

            for (int i = 0; i < 10; i++)
            {
                var helloWorldSerialized = Encoding.UTF8.GetBytes(helloWorld);
                var helloWorldCompressed = proc.Compress(helloWorldSerialized);
                var helloWorldDecompressed = proc.Decompress(helloWorldCompressed);

                var helloWorldProcessed = Encoding.UTF8.GetString(helloWorldDecompressed);
                Assert.Equal(helloWorld, helloWorldProcessed);
            }
        }
    }
}
