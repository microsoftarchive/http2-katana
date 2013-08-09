namespace SharedProtocol.FlowControl
{
    internal enum FlowControlOptions : byte
    {
        UseFlowControl = 0x00,
        UseOnlyStreamsFlowControl = 0x02,
        DontUseFlowControl = 0x03
    }
}
