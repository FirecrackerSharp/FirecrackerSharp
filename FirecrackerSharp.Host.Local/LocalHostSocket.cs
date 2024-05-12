namespace FirecrackerSharp.Host.Local;

public class LocalHostSocket(HttpClient httpClient) : IHostSocket
{
    public async Task<string> GetAsync(string uri)
    {
        return await httpClient.GetStringAsync(uri);
    }

    public async Task PutAsync(string uri, string content)
    {
        await httpClient.PutAsync(uri, new StringContent(content));
    }

    public async Task PatchAsync(string uri, string content)
    {
        await httpClient.PatchAsync(uri, new StringContent(content));
    }
}