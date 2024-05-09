using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FirecrackerSharp;

internal static class InternalUtil
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    internal static Process RunProcess(string command, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = false,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        return process;
    }

    internal static async Task<Process> RunProcessInSudoAsync(string password, string command, string commandArgs)
    {
        var sudoBinary = File.Exists("/usr/bin/su") ? "/usr/bin/su" : "/bin/su";
        var process = RunProcess(sudoBinary, "");
        await process.StandardInput.WriteLineAsync(password);
        await process.StandardInput.WriteLineAsync($"{command} {commandArgs}");
        
        return process;
    }
}