using System.Text;
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

    public bool IsEscalated => connectionInfo.Username == "root";
    
    public Task<IHostProcess> EscalateAndLaunchProcessAsync(string password, string executable, string args)
    {
        var ssh = Ssh;
        var command = ssh.CreateCommand("su");
        var process = new SshHostProcess(command, ssh);
        process.StandardInput.Write(
            new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password + "\n")));
        process.StandardInput.Write(
            new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(executable + " " + args + "\n")));
        return Task.FromResult<IHostProcess>(process);
    }
}