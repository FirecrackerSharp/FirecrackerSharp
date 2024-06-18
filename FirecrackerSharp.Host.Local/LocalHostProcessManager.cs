using System.Diagnostics;

namespace FirecrackerSharp.Host.Local;

internal sealed class LocalHostProcessManager : IHostProcessManager
{
    public IHostProcess LaunchProcess(string executable, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };
        return new LocalHostProcess(process);
    }

    private bool? _hasSudo;
    public bool IsEscalated
    {
        get
        {
            _hasSudo ??= Environment.UserName == "root";
            return _hasSudo.Value;
        }
    }

    public async Task<IHostProcess> EscalateAndLaunchProcessAsync(string password, string executable, string args)
    {
        var sudoBinary = File.Exists("/usr/bin/su") ? "/usr/bin/su" : "/bin/su";
        var osProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = sudoBinary,
                Arguments = "",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        
        var process = new LocalHostProcess(osProcess);

        await process.StdinWriter.WriteLineAsync(password);
        await process.StdinWriter.WriteLineAsync(executable + " " + args);

        return process;
    }
}