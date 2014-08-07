using System.Collections.Generic;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.Exceptions;
using Xunit;

namespace Http2.Katana.Tests
{
    public class EncContextUpdateTests
    {
        private const byte prefix = (byte)UVarIntPrefix.EncodingContextUpdate;
        private const int validHeaderTableSize = 10000;
        private const int invalidHeaderTableSize = 99999;
        private const int newSettingsMaxHeaderTableSize = 15000;

        private CompProcState stateBefore;
        private CompProcState stateAfter;

        /// <summary>
        /// Sends Encoding Context Update with new max headers table size when
        /// SETTINGS_HEADER_TABLE_SIZE is not received
        /// </summary>
        [StandardFact]
        public void SendMaxHeaderTableSize()
        {
            byte[] bytes = validHeaderTableSize.ToUVarInt(prefix);
            bytes[0] |= (byte)IndexationType.EncodingContextUpdate;

            var serverCompressionProc = new CompressionProcessor();

            stateBefore = new CompProcState(serverCompressionProc);

            serverCompressionProc.Decompress(bytes);

            stateAfter = new CompProcState(serverCompressionProc);

            serverCompressionProc.Dispose();

            Assert.Equal(validHeaderTableSize, stateAfter.MaxHeaderByteSize);
            Assert.Equal(stateBefore.SettingsHeaderTableSize, 
                stateAfter.SettingsHeaderTableSize);
            Assert.Equal(stateBefore.WasSettingHeaderTableSizeReceived, 
                stateAfter.WasSettingHeaderTableSizeReceived);
        }

        /// <summary>
        /// Sends Encoding Context Update with invalid new max headers table size when
        /// SETTINGS_HEADER_TABLE_SIZE was already received.
        /// </summary>
        [StandardFact]
        public void SendInvalidMaxHeaderTableSize()
        {
            bool isErrorThrown = false;

            var bytes = invalidHeaderTableSize.ToUVarInt(prefix);
            bytes[0] |= (byte)IndexationType.EncodingContextUpdate;

            var serverCompressionProc = new CompressionProcessor();
            serverCompressionProc.NotifySettingsChanges(newSettingsMaxHeaderTableSize);

            stateBefore = new CompProcState(serverCompressionProc);

            try
            {
                serverCompressionProc.Decompress(bytes);
            }
            catch (CompressionError)
            {
                isErrorThrown = true;
            }

            stateAfter = new CompProcState(serverCompressionProc);

            serverCompressionProc.Dispose();

            Assert.True(isErrorThrown);
            Assert.Equal(newSettingsMaxHeaderTableSize, stateAfter.SettingsHeaderTableSize);
            Assert.Equal(stateBefore.SettingsHeaderTableSize, 
                stateAfter.SettingsHeaderTableSize);
            Assert.Equal(stateBefore.MaxHeaderByteSize, stateBefore.SettingsHeaderTableSize);
            Assert.True(stateAfter.WasSettingHeaderTableSizeReceived);
            Assert.NotEqual(invalidHeaderTableSize, stateAfter.MaxHeaderByteSize);
            Assert.True(stateAfter.MaxHeaderByteSize <= stateAfter.SettingsHeaderTableSize);
            Assert.Equal(stateBefore.MaxHeaderByteSize, stateAfter.MaxHeaderByteSize);
        }

        /// <summary>
        /// Sends Encoding Context Update with valid new max headers table size when
        /// SETTINGS_HEADER_TABLE_SIZE was already received.
        /// </summary>
        [StandardFact]
        public void SendValidMaxHeaderTableSize()
        {
            var  bytes = validHeaderTableSize.ToUVarInt(prefix);
            bytes[0] |= (byte)IndexationType.EncodingContextUpdate;

            var serverCompressionProc = new CompressionProcessor();
            serverCompressionProc.NotifySettingsChanges(newSettingsMaxHeaderTableSize);

            stateBefore = new CompProcState(serverCompressionProc);

            serverCompressionProc.Decompress(bytes);

            stateAfter = new CompProcState(serverCompressionProc);

            serverCompressionProc.Dispose();

            Assert.Equal(newSettingsMaxHeaderTableSize, stateAfter.SettingsHeaderTableSize);
            Assert.Equal(stateBefore.SettingsHeaderTableSize, 
                stateAfter.SettingsHeaderTableSize);
            Assert.Equal(stateBefore.MaxHeaderByteSize, 
                stateBefore.SettingsHeaderTableSize);
            Assert.True(stateAfter.WasSettingHeaderTableSizeReceived == 
                stateBefore.WasSettingHeaderTableSizeReceived);
            Assert.True(stateAfter.WasSettingHeaderTableSizeReceived);
            Assert.Equal(validHeaderTableSize, stateAfter.MaxHeaderByteSize);
            Assert.True(stateAfter.MaxHeaderByteSize <= stateAfter.SettingsHeaderTableSize);
        }
    }
}
