namespace FirecrackerSharp.Host;

public interface IHostProcess
{
    public StreamWriter StdinWriter { get; }
    public string CurrentOutput { get; }

    public event EventHandler<string> OutputReceived; 
    
    public Task KillAsync();
    public Task<bool> WaitForGracefulExitAsync(TimeSpan timeout);
}