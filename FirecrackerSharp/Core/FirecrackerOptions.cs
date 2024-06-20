namespace FirecrackerSharp.Core;

/// <summary>
/// The optional and mandatory options to be passed into the firecracker binary.
/// </summary>
/// <param name="SocketFilename">The filename of the UDS, through which communication to the Management API will occur</param>
/// <param name="SocketDirectory">The path to the directory (either absolute or relative to the chroot jail in the case
/// of a <see cref="JailedVm"/>) of the UDS, through which communication to the Management API will occur</param>
/// <param name="ExtraArguments">Any extra CLI arguments to pass to the firecracker binary. Refer to Firecracker's
/// documentation as to which are possible</param>
public sealed record FirecrackerOptions(
    string SocketFilename,
    string SocketDirectory = "/tmp/firecracker",
    string ExtraArguments = "")
{
    internal string FormatToArguments(string? configPath, string? socketPath)
    {
        var output = ExtraArguments;
        if (socketPath != null)
        {
            output += $" --api-sock {socketPath}";
        }

        if (configPath != null)
        {
            output += $" --config-file {configPath}";
        }

        return output;
    }
}