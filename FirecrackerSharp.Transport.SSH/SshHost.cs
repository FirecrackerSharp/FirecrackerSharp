using FirecrackerSharp.Host;
using Renci.SshNet;

namespace FirecrackerSharp.Transport.SSH;

public class SshHost
{
    public static void Configure(ConnectionInfo connectionInfo)
    {
        IHostFilesystem.Current = new SshHostFilesystem(connectionInfo);
        IHostProcessManager.Current = new SshHostProcessManager(connectionInfo);
    }
}