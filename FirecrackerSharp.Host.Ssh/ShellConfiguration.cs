namespace FirecrackerSharp.Host.Ssh;

public record ShellConfiguration(
    string Terminal,
    uint Columns,
    uint Rows,
    uint Width,
    uint Height,
    int BufferSize,
    string ExpectedShellEnding)
{
    public static readonly ShellConfiguration Default = new(
        "/bin/bash", 1000, 1000, 1000, 1000, 1000, ":~#");
}