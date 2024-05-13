using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

public record ConnectionPoolConfiguration(
    ConnectionInfo ConnectionInfo,
    uint SshConnectionAmount,
    uint SftpConnectionAmount,
    TimeSpan KeepAliveInterval);
