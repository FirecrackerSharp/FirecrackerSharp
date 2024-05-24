namespace FirecrackerSharp.Shells;

public class VmShellCommand
{
    private readonly VmShell _shell;
    private readonly string? _outputFile;
    
    public CaptureMode CaptureMode { get; }
    public Guid Id { get; }
    
    internal VmShellCommand(
        VmShell shell,
        CaptureMode captureMode,
        string? outputFile,
        Guid id)
    {
        _shell = shell;
        CaptureMode = captureMode;
        _outputFile = outputFile;
        Id = id;
    }
}