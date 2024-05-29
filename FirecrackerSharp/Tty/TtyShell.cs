using FirecrackerSharp.Host;

namespace FirecrackerSharp.Tty;

/// <summary>
/// A (bash) shell of a microVM. Multiple shells can coexist despite the limitation of only one TTY due to the fact
/// that GNU screen is used internally as a terminal multiplexer.
/// </summary>
public class TtyShell
{
    public Guid Id { get; }
    
    internal readonly VmTtyShellManager TtyShellManager;
    
    internal TtyShell(VmTtyShellManager ttyShellManager)
    {
        TtyShellManager = ttyShellManager;
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Start and return a new command within this shell.
    /// </summary>
    /// <param name="commandText">The text of the command, not including a newline</param>
    /// <param name="captureMode">The <see cref="CaptureMode"/> for outputs of the command</param>
    /// <param name="exitSignal">The text that should be sent to the command for it to exit, "^C" (Ctrl+C) by default</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for write operations when initializing
    /// the command</param>
    /// <returns>The created and started <see cref="TtyShellCommand"/></returns>
    public async Task<TtyShellCommand> StartCommandAsync(
        string commandText,
        CaptureMode captureMode = CaptureMode.None,
        string exitSignal = "^C",
        CancellationToken cancellationToken = new())
    {
        string? outputFile = null;
        var commandId = Guid.NewGuid();
        var ttyCommand = $"screen -X -p 0 -S {Id} stuff \"{commandText}^M\"";

        if (captureMode != CaptureMode.None)
        {
            const string stdoutDirectory = "/tmp/vm_shell_logs";
            outputFile = $"{stdoutDirectory}/{Id}-{commandId}";
            IHostFilesystem.Current.CreateTextFile(outputFile);
            
            var delimiter = captureMode == CaptureMode.StdoutPlusStderr ? "&>" : ">";
            ttyCommand = $"screen -X -p 0 -S {Id} stuff \"{commandText} {delimiter} {outputFile} ^M\"";
            
            await TtyShellManager.WriteToTtyAsync($"mkdir {stdoutDirectory}", cancellationToken);
        }

        var command = new TtyShellCommand(this, captureMode, outputFile, commandId, exitSignal);

        await TtyShellManager.WriteToTtyAsync(ttyCommand, cancellationToken);

        return command;
    }

    /// <summary>
    /// Quit this shell and remove it from the internal multiplexer.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this operation</param>
    public async Task QuitAsync(CancellationToken cancellationToken = new())
    {
        await TtyShellManager.WriteToTtyAsync($"screen -XS {Id} quit", cancellationToken);
    }
}