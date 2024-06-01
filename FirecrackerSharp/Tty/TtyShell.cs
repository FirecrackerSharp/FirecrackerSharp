using System.Web;

namespace FirecrackerSharp.Tty;

/// <summary>
/// A (bash) shell of a microVM. Multiple shells can coexist despite the limitation of only one TTY due to the fact
/// that GNU screen is used internally as a terminal multiplexer.
/// </summary>
public class TtyShell
{
    /// <summary>
    /// The sequential ID that was generated to uniquely identify this <see cref="TtyShell"/>
    /// </summary>
    public long Id { get; }
    
    internal readonly VmTtyManager TtyManager;
    
    internal TtyShell(VmTtyManager ttyManager, long id)
    {
        TtyManager = ttyManager;
        Id = id;
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
        commandText = commandText.Trim();
        
        string? outputFile = null;
        var ttyCommand = $"screen -X -p 0 -S {Id} stuff '{commandText}^M'";

        TtyManager.LastCommandId++;
        var commandId = TtyManager.LastCommandId;

        if (captureMode != CaptureMode.None)
        {
            const string logDirectory = "/tmp/vm_shell_logs";
            outputFile = $"{logDirectory}/{commandId}";
            
            var delimiter = captureMode == CaptureMode.StdoutPlusStderr ? "&>" : ">";
            ttyCommand = $"screen -X -p 0 -S {Id} stuff '{commandText} {delimiter} {outputFile} ^M'";

            if (!TtyManager.LogDirectoryExists)
            {
                await TtyManager.WriteToTtyAsync($"mkdir {logDirectory}", cancellationToken);
                TtyManager.LogDirectoryExists = true;
            }
        }
        
        var command = new TtyShellCommand(this, captureMode, outputFile, exitSignal, commandId);

        await TtyManager.WriteToTtyAsync(ttyCommand, cancellationToken);

        return command;
    }

    /// <summary>
    /// Quit this shell and remove it from the internal multiplexer.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this operation</param>
    public async Task QuitAsync(CancellationToken cancellationToken = new())
    {
        await TtyManager.WriteToTtyAsync($"screen -XS {Id} quit", cancellationToken);
    }
}