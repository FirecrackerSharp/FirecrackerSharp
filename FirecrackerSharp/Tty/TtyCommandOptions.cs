namespace FirecrackerSharp.Tty;

public record TtyCommandOptions(
    string Command,
    string[] Arguments,
    TimeSpan ReadTimeoutTimeSpan,
    string ExitSignal = "^C",
    bool NewlineAfterExitSignal = true)
{
    internal string ParsedCommand => Command + string.Join(" ", Arguments);
}
