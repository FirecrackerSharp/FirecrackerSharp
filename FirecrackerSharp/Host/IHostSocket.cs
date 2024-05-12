using FirecrackerSharp.Management;

namespace FirecrackerSharp.Host;

public interface IHostSocket : IDisposable
{
    public Task<ManagementResponse> GetAsync<T>(string uri);

    public Task<ManagementResponse> PutAsync<T>(string uri, T content);

    public Task<ManagementResponse> PatchAsync<T>(string uri, T content);
}