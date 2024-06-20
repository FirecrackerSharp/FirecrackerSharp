namespace FirecrackerSharp.Host;

public interface IHostProcess
{
    public string CurrentOutput { get; set; }

    public event EventHandler<string> OutputReceived;

    public Task WriteAsync(string text, CancellationToken cancellationToken);
    public Task WriteLineAsync(string text, CancellationToken cancellationToken);
    public Task KillAsync();
    public Task<bool> WaitForGracefulExitAsync(TimeSpan timeout);
}