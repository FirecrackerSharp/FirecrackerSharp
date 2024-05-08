using System.Diagnostics;
using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Core;

public class FirecrackerVm : IAsyncDisposable
{
    private static readonly ILogger Logger = Log.ForContext(typeof(FirecrackerVm));
    
    private readonly string _socketPath;
    private readonly VmConfiguration _vmConfiguration;
    private readonly FirecrackerInstall _firecrackerInstall;
    private Process? _process;
    private readonly int? _bootWaitSeconds;

    private FirecrackerVm(VmConfiguration vmConfiguration, FirecrackerInstall firecrackerInstall,
        string socketDirectory = "/tmp/firecracker/sockets", string? socketFilename = null, int? bootWaitSeconds = 2)
    {
        socketFilename ??= Guid.NewGuid().ToString();
        _socketPath = Path.Join(socketDirectory, socketFilename + ".sock");
        if (!Directory.Exists(socketDirectory)) Directory.CreateDirectory(socketDirectory);
        
        _vmConfiguration = vmConfiguration;
        _firecrackerInstall = firecrackerInstall;
        _bootWaitSeconds = bootWaitSeconds;
    }

    private async Task InternalStartAsync()
    {
        var configPath = await PrepareForBootAsync();
        _process = _firecrackerInstall.RunFirecracker(
            $" --config-file {configPath} --api-sock {_socketPath}");
        
        if (_bootWaitSeconds.HasValue)
        {
            await Task.Delay(_bootWaitSeconds.Value * 1000);
        }
    }

    private async Task<string> PrepareForBootAsync()
    {
        var configPath = Path.GetTempFileName() + ".json";
        var configJson = JsonSerializer.Serialize(_vmConfiguration, InternalUtil.SerializerOptions);
        await File.WriteAllTextAsync(configPath, configJson);

        return configPath;
    }

    public static async Task<FirecrackerVm> StartAsync(
        VmConfiguration vmConfiguration, FirecrackerInstall firecrackerInstall,
        string socketDirectory = "/tmp/firecracker/sockets", string? socketFilename = null,
        int? bootWaitSeconds = 2)
    {
        var firecrackerVm = new FirecrackerVm(vmConfiguration, firecrackerInstall, socketDirectory, socketFilename, bootWaitSeconds);
        await firecrackerVm.InternalStartAsync();
        return firecrackerVm;
    }

    public async ValueTask DisposeAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(20));
        
        await _process!.StandardInput.WriteLineAsync("reboot");
        try
        {
            await _process.WaitForExitAsync(cancellationTokenSource.Token);
        }
        catch (Exception)
        {
            _process.Kill();
        }
    }
}