namespace FirecrackerSharp.Host;

public interface IHostProcessManager
{
    public static IHostProcessManager Current { internal get; set; } = null!;
    
    public IHostProcess LaunchProcess(string executable, string args);
}