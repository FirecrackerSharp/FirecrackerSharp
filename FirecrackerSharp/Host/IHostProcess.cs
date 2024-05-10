namespace FirecrackerSharp.Host;

public interface IHostProcess
{
    public Stream StandardOutput { get; }
    public Stream StandardInput { get; }

    public void Kill();
    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken);
}