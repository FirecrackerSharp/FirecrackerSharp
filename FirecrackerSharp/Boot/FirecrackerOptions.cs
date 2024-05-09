namespace FirecrackerSharp.Boot;

public record FirecrackerOptions(
    string SocketFilename,
    string SocketDirectory = "/tmp/firecracker/sockets",
    string ExtraArguments = "",
    int? WaitSecondsAfterBoot = 2)
{
    internal string FormatToArguments(string configPath, string? socketPath)
    {
        var output = $"--config-file {configPath} {ExtraArguments}";
        if (socketPath != null)
        {
            output += $" --api-sock {socketPath}";
        }

        return output;
    }
}