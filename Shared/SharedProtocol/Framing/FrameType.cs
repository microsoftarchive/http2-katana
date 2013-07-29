
namespace SharedProtocol.Framing
{
    /// <summary>
    /// Frame type enum.
    /// </summary>
    public enum FrameType : byte
    {
        Data = 0,
        Headers = 1,
        Priority = 2,
        RstStream = 3,
        Settings = 4,
        PushPromise = 5,
        Ping = 6,
        GoAway = 7,
        //8?
        WindowUpdate = 9
    }
}
