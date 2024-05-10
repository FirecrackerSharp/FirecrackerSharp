using FirecrackerSharp.Host;
using Renci.SshNet;

namespace FirecrackerSharp.Transport.SSH;

internal class SshHostProcess(SshCommand sshCommand, IBaseClient sshClient) : IHostProcess
{
    public Stream StandardOutput => sshCommand.OutputStream;
    
    private Stream? _inputStream;
    public Stream StandardInput => _inputStream ??= sshCommand.CreateInputStream();

    private readonly IAsyncResult _executionResult = sshCommand.BeginExecute();

    public void Kill()
    {
        sshCommand.CancelAsync();
        DisposeSshClient();
    }

    public async Task WaitUntilCompletionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_executionResult.IsCompleted || _executionResult.CompletedSynchronously) return;
            await Task.Delay(15, cancellationToken);
        }
        DisposeSshClient();
    }

    private void DisposeSshClient()
    {
        sshClient.Disconnect();
        sshClient.Dispose();
    }
}