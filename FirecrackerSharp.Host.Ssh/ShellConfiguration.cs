using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

/// <summary>
/// The configuration options for creating SSH PTYs (emulated Linux terminals), which are used internally by the SSH
/// host in order to launch and manage the interactive Firecracker process (including TTY interactions). This is
/// implemented via SSH.NET's <see cref="ShellStream"/> so refer to the documentation of that for more details.
/// </summary>
/// <param name="Terminal">The name of the terminal to be used</param>
/// <param name="Columns">The amount of columns in the terminal</param>
/// <param name="Rows">The amount of rows in the terminal</param>
/// <param name="Width">The width of the terminal</param>
/// <param name="Height">The height of the terminal</param>
/// <param name="BufferSize">The buffer size of the terminal</param>
/// <param name="ExpectedShellEnding">The expected ending of this shell. This is used in order to detect when the
/// Firecracker process has terminated</param>
public sealed record ShellConfiguration(
    string Terminal,
    uint Columns,
    uint Rows,
    uint Width,
    uint Height,
    int BufferSize,
    string ExpectedShellEnding)
{
    /// <summary>
    /// A default <see cref="ShellConfiguration"/> usable for most cases. Uses Bash with 1000x1000 columns and rows,
    /// 1000x1000 size and a ":~#" expected ending.
    /// </summary>
    public static readonly ShellConfiguration Default = new(
        "/bin/bash", 1000, 1000, 1000, 1000, 1000, ":~#");
}