namespace FirecrackerSharp.Shells;

public class VmShell
{
    public Guid Id { get; }
    
    internal readonly VmShellManager ShellManager;
    
    internal VmShell(VmShellManager shellManager)
    {
        ShellManager = shellManager;
        Id = Guid.NewGuid();
    }

    public async Task<VmShellCommand> StartCommandAsync(
        string commandText,
        CaptureMode captureMode = CaptureMode.None,
        string exitSignal = "^C",
        CancellationToken writeCancellationToken = new())
    {
        string? outputFile = null;
        var commandId = Guid.NewGuid();
        var ttyCommand = $"screen -X -p 0 -S {Id} stuff \"{commandText}^M\"";

        if (captureMode != CaptureMode.None)
        {
            const string stdoutDirectory = "/tmp/vm_shell_logs";
            outputFile = $"{stdoutDirectory}/{Id}-{commandId}";
            
            var delimiter = captureMode == CaptureMode.StdoutPlusStderr ? "&>" : ">";
            ttyCommand = $"screen -X -p 0 -S {Id} stuff \"{commandText} {delimiter} {outputFile} ^M\"";
            
            await ShellManager.WriteToTtyAsync($"mkdir {stdoutDirectory}", writeCancellationToken);
        }

        var command = new VmShellCommand(this, captureMode, outputFile, commandId, exitSignal);

        await ShellManager.WriteToTtyAsync(ttyCommand, writeCancellationToken);

        return command;
    }

    public async Task QuitAsync()
    {
        await ShellManager.WriteToTtyAsync($"screen -XS {Id} quit", new CancellationToken());
    }
}