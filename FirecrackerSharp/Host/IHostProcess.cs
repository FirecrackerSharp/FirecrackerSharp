namespace FirecrackerSharp.Host;

public interface IHostProcess
{
    public StreamReader StdoutReader { get; }
    public StreamWriter StdinWriter { get; }

    public Task<string> KillAndReadAsync();
    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken);
}