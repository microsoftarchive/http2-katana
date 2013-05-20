
namespace SharedProtocol.Framing
{
    public enum FrameType : byte
    {
        Data = 0,
        HeadersPlusPriority = 1,
        // 2?
        RstStream = 3,
        Settings = 4,
        PushPromise = 5,
        Ping = 6,
        GoAway = 7,
        Headers = 8,
        WindowUpdate = 9
    }
}
