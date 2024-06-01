using System.Text;

namespace FirecrackerSharp.Tty;

/// <summary>
/// A command that was started inside a <see cref="TtyShell"/>.
/// </summary>
public class TtyShellCommand : IAsyncDisposable
{
    private readonly TtyShell _shell;
    private readonly string? _outputFile;
    
    /// <summary>
    /// The <see cref="CaptureMode"/> that was assigned to this <see cref="TtyShellCommand"/> upon creation.
    /// </summary>
    public CaptureMode CaptureMode { get; }
    /// <summary>
    /// The sequential ID that was generated to uniquely identify this <see cref="TtyShellCommand"/>.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// The exit signal that was assigned to this <see cref="TtyShellCommand"/> upon creation.
    /// </summary>
    public string ExitSignal { get; }
    
    internal TtyShellCommand(
        TtyShell shell,
        CaptureMode captureMode,
        string? outputFile,
        string exitSignal,
        long id)
    {
        _shell = shell;
        CaptureMode = captureMode;
        _outputFile = outputFile;
        ExitSignal = exitSignal;
        Id = id;
    }

    /// <summary>
    /// Capture the output of this <see cref="TtyShellCommand"/> according to what was specified in the <see cref="CaptureMode"/>
    /// for it.
    ///
    /// If the <see cref="CaptureMode"/> is "None" or no output was produced, null will be returned.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this operation</param>
    /// <returns>The captured output or null if it wasn't captured for a certain reason</returns>
    public async Task<string?> CaptureOutputAsync(CancellationToken cancellationToken = new())
    {
        if (CaptureMode == CaptureMode.None) return null;

        await _shell.TtyManager.ReadFromTtyAsync(cancellationToken);
        
        var originalCommand = $"cat {_outputFile}";
        await _shell.TtyManager.WriteToTtyAsync(originalCommand, cancellationToken, subsequentlyRead: false);

        var capturedOutput = await _shell.TtyManager.ReadFromTtyAsync(cancellationToken);
        if (capturedOutput is null) return null;
        
        var capturedOutputBuilder = new StringBuilder();

        foreach (var line in capturedOutput
                     .Split("\n")
                     .Select(x => x.TrimEnd()))
        {
            if (!line.StartsWith(originalCommand))
            {
                capturedOutputBuilder.AppendLine(line);
            }
        }

        return capturedOutputBuilder.ToString().Trim();
    }
    
    /// <summary>
    /// Cancel this <see cref="TtyShellCommand"/> by sending it its <see cref="ExitSignal"/>.
    ///
    /// While this works fine for various cases, in a more parallelized scenario (dozens of shells and commands in each
    /// shell) this may cause slight alteration of captured output, which is why it's not yet stable for the
    /// time being.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this operation</param>
    public async Task CancelAsync(CancellationToken cancellationToken = new())
    {
        await _shell.TtyManager.WriteToTtyAsync($"screen -X -p 0 -S {_shell.Id} stuff '{ExitSignal}'", cancellationToken);
    }

    /// <summary>
    /// Send an input to the standard input stream of this <see cref="TtyShellCommand"/> (stdin).
    /// </summary>
    /// <param name="input">The inputted value</param>
    /// <param name="insertNewline">Whether to insert a newline after the inputted value (true by default)</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this operation</param>
    public async Task SendStdinAsync(
        string input,
        bool insertNewline = true,
        CancellationToken cancellationToken = new())
    {
        var newlineSymbol = insertNewline ? "^M" : "";
        await _shell.TtyManager.WriteToTtyAsync(
            $"screen -X -p 0 -S {_shell.Id} stuff '{input}{newlineSymbol}'", cancellationToken, subsequentlyRead: false);
    }

    /// <summary>
    /// Disposes of this <see cref="TtyShellCommand"/>'s captured output file if one has been created in accordance with
    /// the given <see cref="CaptureMode"/>
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_outputFile != null)
        {
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await _shell.TtyManager.WriteToTtyAsync($"rm {_outputFile}", tokenSource.Token);
        }
    }
}