using System.Text;

namespace FirecrackerSharp.Host.Ssh;

internal class SshHostProcessManager(ConnectionPool connectionPool) : IHostProcessManager
{
    public IHostProcess LaunchProcess(string executable, string args)
    {
        var ssh = connectionPool.NewUnmanagedSshConnection();
        var command = ssh.CreateCommand(executable + " " + args);
        return new SshHostProcess(command, ssh);
    }

    public bool IsEscalated => connectionPool.ConnectionInfo.Username == "root";
    
    public Task<IHostProcess> EscalateAndLaunchProcessAsync(string password, string executable, string args)
    {
        var ssh = connectionPool.NewUnmanagedSshConnection();
        var command = ssh.CreateCommand("su");
        var process = new SshHostProcess(command, ssh);
        process.StandardInput.Write(
            new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password + "\n")));
        process.StandardInput.Write(
            new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(executable + " " + args + "\n")));
        return Task.FromResult<IHostProcess>(process);
    }
}