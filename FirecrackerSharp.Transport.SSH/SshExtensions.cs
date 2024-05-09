using Renci.SshNet;

namespace FirecrackerSharp.Transport.SSH;

internal static class SshExtensions
{
    internal static SshClient Connect(this SshClient sshClient)
    {
        sshClient.Connect();
        return sshClient;
    }

    internal static SftpClient Connect(this SftpClient sftpClient)
    {
        sftpClient.Connect();
        return sftpClient;
    }
}