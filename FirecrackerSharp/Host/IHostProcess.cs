namespace FirecrackerSharp.Host;

public interface IHostProcess
{
    public StreamReader StdoutReader { get; }
    public StreamWriter StdinWriter { get; }

    public void Kill();
    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken);
}