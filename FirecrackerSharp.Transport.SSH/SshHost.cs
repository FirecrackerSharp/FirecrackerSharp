using FirecrackerSharp.Host;
using Renci.SshNet;
using Serilog;

namespace FirecrackerSharp.Transport.SSH;

public class SshHost
{
    public static void Configure(ConnectionInfo connectionInfo)
    {
        IHostFilesystem.Current = new SshHostFilesystem(connectionInfo);
        IHostProcessManager.Current = new SshHostProcessManager(connectionInfo);
        
        Log.Information("Using remote SSH host for FirecrackerSharp");
    }
}