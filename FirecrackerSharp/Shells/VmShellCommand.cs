using System.Text;

namespace FirecrackerSharp.Shells;

public class VmShellCommand : IAsyncDisposable
{
    private readonly VmShell _shell;
    private readonly string? _outputFile;
    
    public CaptureMode CaptureMode { get; }
    public Guid Id { get; }
    public string ExitSignal { get; }
    
    internal VmShellCommand(
        VmShell shell,
        CaptureMode captureMode,
        string? outputFile,
        Guid id,
        string exitSignal)
    {
        _shell = shell;
        CaptureMode = captureMode;
        _outputFile = outputFile;
        Id = id;
        ExitSignal = exitSignal;
    }

    public async Task<string?> CaptureOutputAsync(CancellationToken cancellationToken = new())
    {
        if (CaptureMode == CaptureMode.None) return null;

        await _shell.ShellManager.ReadFromTtyAsync(cancellationToken);
        await _shell.ShellManager.WriteToTtyAsync($"cat {_outputFile}", cancellationToken, subsequentlyRead: false);

        var capturedOutput = await _shell.ShellManager.ReadFromTtyAsync(cancellationToken);
        if (capturedOutput is null) return null;
        
        var capturedOutputBuilder = new StringBuilder();

        foreach (var line in capturedOutput
                     .Split("\n")
                     .Select(x => x.TrimEnd()))
        {
            if (!line.EndsWith('#') && !line.StartsWith('<'))
            {
                capturedOutputBuilder.AppendLine(line);
            }
        }

        return capturedOutputBuilder.ToString().Trim();
    }

    public async Task CancelAsync(CancellationToken cancellationToken = new())
    {
        await _shell.ShellManager.WriteToTtyAsync($"screen -X -p 0 -S {_shell.Id} stuff \"{ExitSignal}\"", cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _shell.ShellManager.WriteToTtyAsync($"rm {_outputFile}", new CancellationToken());
    }
}