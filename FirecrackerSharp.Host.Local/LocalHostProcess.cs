using System.Diagnostics;

namespace FirecrackerSharp.Host.Local;

internal class LocalHostProcess(Process osProcess) : IHostProcess
{
    public StreamReader StdoutReader => osProcess.StandardOutput;
    public StreamWriter StdinWriter => osProcess.StandardInput;

    public async Task<string> KillAndReadAsync()
    {
        osProcess.Kill();
        var result = await StdoutReader.ReadToEndAsync();
        return result;
    }

    public Task WaitUntilCompletionAsync(CancellationToken cancellationToken)
    {
        return osProcess.WaitForExitAsync(cancellationToken);
    }
}