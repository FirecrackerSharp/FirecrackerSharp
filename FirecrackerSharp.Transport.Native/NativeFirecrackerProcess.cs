using System.Diagnostics;

namespace FirecrackerSharp.Transport.Native;

public class NativeFirecrackerProcess(Process osProcess) : IFirecrackerProcess
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