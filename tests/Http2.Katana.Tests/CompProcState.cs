using System;
using System.Reflection;
using Microsoft.Http2.Protocol;
using Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression;

namespace Http2.Katana.Tests
{
    /// <summary>
    /// Class contains the fields of Compression Processor 
    /// which are changed by Encoding Context Update
    /// </summary>
    internal class CompProcState
    {
        public bool IsSettingHeaderTableSizeReceived { get; private set; }
        public int SettingsHeaderTableSize { get; private set; }
        public int MaxHeaderByteSize { get; private set; }
        public HeadersList LocalRefSet { get; private set; }

        public CompProcState(CompressionProcessor proc)
        {
            IsSettingHeaderTableSizeReceived = (bool)GetFieldValue(typeof(CompressionProcessor),
                "_isSettingHeaderTableSizeReceived", proc);
            SettingsHeaderTableSize = (int)GetFieldValue(typeof(CompressionProcessor),
                "_settingsHeaderTableSize", proc);
            MaxHeaderByteSize = (int)GetFieldValue(typeof(CompressionProcessor),
                "_maxHeaderByteSize", proc);
            LocalRefSet = (HeadersList)GetFieldValue(typeof(CompressionProcessor),
                "_localRefSet", proc);
        }

        private object GetFieldValue(Type type, string fieldName, object instance)
        {
            return type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        }
    }
}
