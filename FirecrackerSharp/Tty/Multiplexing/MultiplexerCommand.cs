namespace FirecrackerSharp.Tty.Multiplexing;

public sealed class MultiplexerCommand
{
    private readonly VmTtyClient _ttyClient;
    private readonly MultiplexerSession _session;
    
    public long Id { get; }
    public MultiplexedCaptureMode CaptureMode { get; }
    public string? CapturePath { get; }

    internal MultiplexerCommand(VmTtyClient ttyClient, MultiplexerSession session, MultiplexedCaptureMode captureMode,
        string? capturePath, long id)
    {
        _ttyClient = ttyClient;
        _session = session;
        CaptureMode = captureMode;
        CapturePath = capturePath;
        Id = id;
    }
}