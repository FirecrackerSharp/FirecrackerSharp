using FirecrackerSharp.Data;

namespace FirecrackerSharp.Boot;

/// <summary>
/// The optional and mandatory options to be passed into the firecracker binary.
/// </summary>
/// <param name="SocketFilename">The filename of the UDS, through which communication to the Management API will occur</param>
/// <param name="SocketDirectory">The path to the directory (either absolute or relative to the chroot jail in the case
/// of a <see cref="JailedVm"/>) of the UDS, through which communication to the Management API will occur</param>
/// <param name="ExtraArguments">Any extra CLI arguments to pass to the firecracker binary. Refer to Firecracker's
/// documentation as to which are possible</param>
/// <param name="WaitMillisForSocketInitialization">How many milliseconds to wait for the API socket to become
/// available in case the <see cref="VmConfigurationApplicationMode"/> is not through a JSON configuration</param>
/// <param name="WaitMillisAfterBoot">How many milliseconds to wait after instantiating the firecracker/jailer process
/// in order for the microVM to boot through the init system (openrc, systemd, runc etc.), or null if no waiting
/// should occur (not recommended to avoid prematurely contacting the microVM)</param>
public record FirecrackerOptions(
    string SocketFilename,
    string SocketDirectory = "/tmp/firecracker",
    string ExtraArguments = "",
    uint WaitMillisForSocketInitialization = 200,
    uint WaitMillisAfterBoot = 1500)
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