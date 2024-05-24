namespace FirecrackerSharp.Shells;

public class VmShell
{
    public Guid Id { get; }
    
    private readonly VmShellManager _shellManager;
    
    internal VmShell(VmShellManager shellManager)
    {
        _shellManager = shellManager;
        Id = Guid.NewGuid();
    }

    public async Task<VmShellCommand> StartCommandAsync(
        string commandText,
        CaptureMode captureMode = CaptureMode.None,
        CancellationToken writeCancellationToken = new())
    {
        string? outputFile = null;
        var commandId = Guid.NewGuid();
        var ttyCommand = $"screen -X -p 0 -S {Id} stuff \"{commandText}^M\"";

        if (captureMode != CaptureMode.None)
        {
            var stdoutDirectory = $"/tmp/vm_shell_logs/{Id}";
            outputFile = $"{stdoutDirectory}/{commandId}";
            
            var delimiter = captureMode == CaptureMode.StdoutPlusStderr ? "&>" : ">";
            ttyCommand = $"screen -X -p 0 -S {Id} stuff \"{commandText} {delimiter} {outputFile}\"";
            
            await _shellManager.WriteToTtyAsync($"mkdir {stdoutDirectory}", writeCancellationToken);
        }

        var command = new VmShellCommand(this, captureMode, outputFile, commandId);

        await _shellManager.WriteToTtyAsync(ttyCommand, writeCancellationToken);

        return command;
    }
}