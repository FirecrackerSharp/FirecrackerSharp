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
    
    public async Task<IHostProcess> EscalateAndLaunchProcessAsync(string password, string executable, string args)
    {
        var ssh = connectionPool.NewUnmanagedSshConnection();
        var command = ssh.CreateCommand("su");
        var process = new SshHostProcess(command, ssh);
        await process.InputWriter.WriteLineAsync(password);
        await process.InputWriter.WriteLineAsync(executable + " " + args);
        return process;
    }
}