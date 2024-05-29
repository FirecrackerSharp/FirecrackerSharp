namespace FirecrackerSharp.Tty;

/// <summary>
/// The mode of capturing (piping out) output streams of shell commands.
/// </summary>
public enum CaptureMode
{
    /// <summary>
    /// Do not capture any output of a command
    /// </summary>
    None,
    /// <summary>
    /// Only capture standard output (stdout)
    /// </summary>
    Stdout,
    /// <summary>
    /// Capture both standard output and error (stdout, stderr)
    /// </summary>
    StdoutPlusStderr
}