namespace FirecrackerSharp.Host;

public interface IHostProcess
{
    public StreamReader OutputReader { get; }
    public StreamWriter InputWriter { get; }

    public void Kill();
    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken);
}