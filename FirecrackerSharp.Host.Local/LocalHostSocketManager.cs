using System.Net.Sockets;

namespace FirecrackerSharp.Host.Local;

internal sealed class LocalHostSocketManager : IHostSocketManager
{
    public IHostSocket Connect(string socketPath, string baseUri)
    {
        if (!File.Exists(socketPath))
        {
            throw new SocketDoesNotExistException($"The socket at \"{socketPath}\" does not exist");
        }
        
        var httpClient = new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = async (_, token) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var endpoint = new UnixDomainSocketEndPoint(socketPath);
                await socket.ConnectAsync(endpoint, token);
                return new NetworkStream(socket, ownsSocket: true);
            }
        })
        {
            BaseAddress = new Uri(baseUri)
        };
        return new LocalHostSocket(httpClient);
    }
}