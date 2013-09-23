namespace Http2.TestClient
{
    internal enum CommandType : byte
    {
        None = 0,
        Connect = 1,
        Get = 2,
        Disconnect = 3,
        CaptureStatsOn = 4,
        CaptureStatsOff = 5,
        Dir = 6,
        Exit = 7,
        Help = 8,
        Unknown = 9,
        Empty = 10,
        Ping = 11,
        Post = 12,
        Put = 13,
        Delete = 14,
    }
}
