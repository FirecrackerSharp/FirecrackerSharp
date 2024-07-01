namespace FirecrackerSharp.Tty.Multiplexing;

public sealed class TtyMultiplexerCommand
{
    public ulong Id { get; }
    public CaptureMode CaptureMode { get; }
    public string CommandText { get; }

    private readonly string? _capturePath;

    internal TtyMultiplexerCommand(ulong id, CaptureMode captureMode, string commandText, string? capturePath)
    {
        Id = id;
        CaptureMode = captureMode;
        CommandText = commandText;
        _capturePath = capturePath;
    }
}