using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

/// <summary>
/// The configuration options for an SSH and SFTP connection pool.
/// </summary>
/// <param name="ConnectionInfo">The SSH.NET <see cref="ConnectionInfo"/> to specify connection authentication options</param>
/// <param name="SshConnectionAmount">The amount of concurrent SSH connections that should be kept active</param>
/// <param name="SftpConnectionAmount">The amount of concurrent SFTP connections that should be kept active</param>
/// <param name="KeepAliveInterval">The <see cref="TimeSpan"/> of sending a keep-alive request to an SSH/SFTP connection</param>
public sealed record ConnectionPoolConfiguration(
    ConnectionInfo ConnectionInfo,
    uint SshConnectionAmount,
    uint SftpConnectionAmount,
    TimeSpan KeepAliveInterval);
