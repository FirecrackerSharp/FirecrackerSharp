using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

internal class SshHostProcess(SshCommand sshCommand, IBaseClient sshClient) : IHostProcess
{
    private Stream? _inputStream;
    private readonly IAsyncResult _executionResult = sshCommand.BeginExecute();

    public StreamReader StdoutReader => new(sshCommand.ExtendedOutputStream);

    public StreamWriter StdinWriter
    {
        get
        {
            _inputStream ??= sshCommand.CreateInputStream();
            return new StreamWriter(_inputStream);
        }
    }

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