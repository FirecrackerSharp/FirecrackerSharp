using FirecrackerSharp.Host;
using Renci.SshNet;

namespace FirecrackerSharp.Transport.SSH;

internal class SshHostProcessManager(ConnectionInfo connectionInfo) : IHostProcessManager
{
    private SshClient Ssh
    {
        get
        {
            var client = new SshClient(connectionInfo);
            client.Connect();
            return client;
        }
    }
    
    public IHostProcess LaunchProcess(string executable, string args)
    {
        var ssh = Ssh;
        var command = ssh.CreateCommand(executable + " " + args);
        return new SshHostProcess(command, ssh);
    }
}