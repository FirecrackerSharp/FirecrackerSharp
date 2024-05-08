using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FirecrackerSharp.Installation;

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
    
    internal static Process RunFirecracker(this FirecrackerInstall install, string args)
    {
        return RunProcess(install.FirecrackerBinary, args);
    }

    internal static Process RunJailer(this FirecrackerInstall install, string args)
    {
        return RunProcess(install.JailerBinary, args);
    }
    
    internal static Process RunProcess(string command, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        return process;
    }
}