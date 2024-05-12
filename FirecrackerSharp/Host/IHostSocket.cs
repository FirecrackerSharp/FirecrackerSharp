namespace FirecrackerSharp.Host;

public interface IHostSocket
{
    public Task<string> GetAsync(string uri);

    public Task PutAsync(string uri, string content);

    public Task PatchAsync(string uri, string content);
}