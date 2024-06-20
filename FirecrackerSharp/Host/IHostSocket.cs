using FirecrackerSharp.Management;

namespace FirecrackerSharp.Host;

public interface IHostSocket : IDisposable
{
    public Task<ResponseWith<TReceived>> GetAsync<TReceived>(string uri, CancellationToken cancellationToken) where TReceived : class;

    public Task<Response> PutAsync<TSent>(string uri, TSent content, CancellationToken cancellationToken) where TSent : class;

    public Task<Response> PatchAsync<TSent>(string uri, TSent content, CancellationToken cancellationToken) where TSent : class;
}