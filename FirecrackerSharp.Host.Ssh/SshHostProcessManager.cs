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
        throw new Exception("Manual escalation is not supported on the SSH host!");
    }
}