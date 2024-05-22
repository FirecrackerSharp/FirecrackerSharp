using System.Diagnostics;

namespace FirecrackerSharp.Host.Local;

internal class LocalHostProcess(Process osProcess) : IHostProcess
{
    public StreamReader OutputReader => osProcess.StandardOutput;
    public StreamWriter InputWriter => osProcess.StandardInput;

    public void Kill()
    {
        osProcess.Kill();
    }

    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken)
    {
        return osProcess.WaitForExitAsync(cancellationToken);
    }
}