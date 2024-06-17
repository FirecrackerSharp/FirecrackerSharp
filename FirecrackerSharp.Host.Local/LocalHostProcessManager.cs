using System.Diagnostics;

namespace FirecrackerSharp.Host.Local;

internal class LocalHostProcessManager : IHostProcessManager
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
        process.Start();
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
        var sudoBinary = File.Exists("/usr/bin/sudo") ? "/usr/bin/sudo" : "/bin/sudo";
        var osProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = sudoBinary,
                Arguments = "-s",
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