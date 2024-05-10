namespace FirecrackerSharp.Host;

public interface IHostProcessManager
{
    public static IHostProcessManager Current { get; set; } = null!;
    
    public IHostProcess LaunchProcess(string executable, string args);
    
    public bool IsEscalated { get; }

    public Task<IHostProcess> EscalateAndLaunchProcessAsync(string password, string executable, string args);
}