namespace FirecrackerSharp.Transport;

public interface IFirecrackerProcess
{
    public Stream StandardOutput { get; }
    public Stream StandardInput { get; }

    public void Kill();
    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken);
}