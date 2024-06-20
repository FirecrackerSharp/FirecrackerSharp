namespace FirecrackerSharp.Host.Ssh;

internal sealed class SshHostProcessManager(ConnectionPool connectionPool, ShellConfiguration shellConfiguration) : IHostProcessManager
{
    public IHostProcess LaunchProcess(string executable, string args)
    {
        var ssh = connectionPool.NewUnmanagedSshConnection();
        var shellStream = ssh.CreateShellStream(
            shellConfiguration.Terminal,
            shellConfiguration.Columns,
            shellConfiguration.Rows,
            shellConfiguration.Width,
            shellConfiguration.Height,
            shellConfiguration.BufferSize);
        shellStream.WriteLine(executable + " " + args);
        return new SshHostProcess(shellStream, ssh);
    }

    public bool IsEscalated => connectionPool.ConnectionInfo.Username == "root";
    
    public Task<IHostProcess> EscalateAndLaunchProcessAsync(string password, string executable, string args)
    {
        throw new Exception("Manual escalation is not supported on the SSH host!");
    }
}