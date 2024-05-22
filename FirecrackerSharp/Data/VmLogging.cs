using FirecrackerSharp.Host;

namespace FirecrackerSharp.Data;

public record VmLogging(
    bool Enabled = false,
    bool OnlyLogLifecycle = true,
    string Directory = "/tmp/firecracker/logs",
    string Filename = "current_log")
{
    internal string LogPath => IHostFilesystem.Current.JoinPaths(Directory, Filename);
}