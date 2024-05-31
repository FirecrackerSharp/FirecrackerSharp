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
        var suBinary = File.Exists("/usr/bin/su") ? "/usr/bin/su" : "/bin/su";
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = suBinary,
                Arguments = "",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();

        await Task.Delay(2000);
        await process.StandardInput.WriteLineAsync(password);
        await Task.Delay(2000);
        await process.StandardInput.WriteLineAsync(executable + " " + args);

        return new LocalHostProcess(process);
    }
}