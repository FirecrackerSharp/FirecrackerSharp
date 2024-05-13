using System.Diagnostics;

namespace FirecrackerSharp.Host.Local;

internal class LocalHostProcess(Process osProcess) : IHostProcess
{
    public Stream StandardOutput => osProcess.StandardOutput.BaseStream;
    public Stream StandardInput => osProcess.StandardInput.BaseStream;
    
    public void Kill()
    {
        osProcess.Kill();
    }

    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken)
    {
        return osProcess.WaitForExitAsync(cancellationToken);
    }
}