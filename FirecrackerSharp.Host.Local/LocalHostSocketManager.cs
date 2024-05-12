using System.Net.Sockets;

namespace FirecrackerSharp.Host.Local;

public class LocalHostSocketManager : IHostSocketManager
{
    public IHostSocket Connect(string socketAddress, string baseAddress)
    {
        var httpClient = new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = async (_, token) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var endpoint = new UnixDomainSocketEndPoint(socketAddress);
                await socket.ConnectAsync(endpoint, token);
                return new NetworkStream(socket, ownsSocket: true);
            }
        })
        {
            BaseAddress = new Uri(baseAddress)
        };
        return new LocalHostSocket(httpClient);
    }
}