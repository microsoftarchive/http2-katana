using System.Collections.Generic;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;
using Microsoft.Http2.Protocol.Extensions;
using Microsoft.Http2.Protocol.Exceptions;
using Xunit;

namespace Http2.Katana.Tests
{
    public class HeaderTableSizeUpdateTests
    {
        private const byte prefix = (byte)UVarIntPrefix.HeaderTableSizeUpdate;
        private const int validHeaderTableSize = 10000;
        private const int invalidHeaderTableSize = 99999;
        private const int newSettingsMaxHeaderTableSize = 15000;

        private readonly int[] severalMaxHeaderTableSizeSettings = new int[] { 7000, 8000, 6000};

        private CompProcState stateBefore;
        private CompProcState stateAfter;

        /// <summary>
        /// Sends Header Table Size Update with new max headers table size when
        /// SETTINGS_HEADER_TABLE_SIZE is not received
        /// </summary>
        [StandardFact]
        public void SendMaxHeaderTableSize()
        {
            byte[] bytes = validHeaderTableSize.ToUVarInt(prefix);
            bytes[0] |= (byte)IndexationType.HeaderTableSizeUpdate;

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
        /// Sends Header Table Size Update with invalid new max headers table size when
        /// SETTINGS_HEADER_TABLE_SIZE was already received.
        /// </summary>
        [StandardFact]
        public void SendInvalidMaxHeaderTableSize()
        {
            bool isErrorThrown = false;

            var bytes = invalidHeaderTableSize.ToUVarInt(prefix);
            bytes[0] |= (byte)IndexationType.HeaderTableSizeUpdate;

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
        /// Sends Header Table Size Update with valid new max headers table size when
        /// SETTINGS_HEADER_TABLE_SIZE was already received.
        /// </summary>
        [StandardFact]
        public void SendValidMaxHeaderTableSize()
        {
            var  bytes = validHeaderTableSize.ToUVarInt(prefix);
            bytes[0] |= (byte)IndexationType.HeaderTableSizeUpdate;

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

        /// <summary>
        /// Sends several Header Table Size Updates.
        /// </summary>
        [StandardFact]
        public void SendSeveralHeaderTableSizeUpdates()
        {
            var serverCompressionProc = new CompressionProcessor();

            foreach (var settingSize in severalMaxHeaderTableSizeSettings)
            {
                serverCompressionProc.NotifySettingsChanges(settingSize);
            }

            var serverState = new CompProcState(serverCompressionProc);

            var headers = new HeadersList() { new KeyValuePair<string, string>("key", "value") };
            var bytes = serverCompressionProc.Compress(headers);

            var clientCompressionProc = new CompressionProcessor();
            var resultHeaders = clientCompressionProc.Decompress(bytes);
            var clientState = new CompProcState(clientCompressionProc);

            Assert.Equal(resultHeaders.Count, headers.Count);
            for (int i = 0; i < headers.Count; i++)
            {
                Assert.Equal(headers[i].Key, resultHeaders[i].Key);
                Assert.Equal(headers[i].Value, resultHeaders[i].Value);
            }

            Assert.Equal(severalMaxHeaderTableSizeSettings[severalMaxHeaderTableSizeSettings.Length - 1],
                clientState.MaxHeaderByteSize);
            Assert.Equal(severalMaxHeaderTableSizeSettings[severalMaxHeaderTableSizeSettings.Length - 1],
                serverState.SettingsHeaderTableSize);
            Assert.Equal(severalMaxHeaderTableSizeSettings[severalMaxHeaderTableSizeSettings.Length - 1],
                serverState.MaxHeaderByteSize);
        }
    }
}
