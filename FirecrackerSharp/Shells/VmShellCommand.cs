namespace FirecrackerSharp.Shells;

public class VmShellCommand : IAsyncDisposable
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

    public async Task<string?> GetCapturedOutputAsync(
        CancellationToken cancellationToken = new())
    {
        if (CaptureMode == CaptureMode.None) return null;

        await _shell.ShellManager.ReadFromTtyAsync(cancellationToken);
        await _shell.ShellManager.WriteToTtyAsync($"cat {_outputFile}", cancellationToken);

        var capturedOutput = await _shell.ShellManager.ReadFromTtyAsync(cancellationToken, linesToSkip: 2);
        return capturedOutput;
    }

    public async ValueTask DisposeAsync()
    {
        await _shell.ShellManager.WriteToTtyAsync($"rm {_outputFile}", new CancellationToken());
    }
}