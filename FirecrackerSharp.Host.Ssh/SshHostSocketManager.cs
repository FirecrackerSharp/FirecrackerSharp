namespace FirecrackerSharp.Host.Ssh;

internal class SshHostSocketManager(ConnectionPool connectionPool, CurlConfiguration curlConfiguration) : IHostSocketManager
{
    public IHostSocket Connect(string socketAddress, string baseAddress)
        => new SshHostSocket(connectionPool, curlConfiguration, baseAddress, socketAddress);
}