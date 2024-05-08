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
    private readonly Guid _vmId = Guid.NewGuid();

    private FirecrackerVm(VmConfiguration vmConfiguration, FirecrackerInstall firecrackerInstall,
        string socketDirectory = "/tmp/firecracker/sockets", string? socketFilename = null, int? bootWaitSeconds = 2)
    {
        socketFilename ??= Guid.NewGuid().ToString();
        _socketPath = Path.Join(socketDirectory, socketFilename + ".sock");
        if (!Directory.Exists(socketDirectory)) Directory.CreateDirectory(socketDirectory);
        
        Logger.Debug("The Unix socket for the VM will be created at: {socketPath}", _socketPath);
        
        _vmConfiguration = vmConfiguration;
        _firecrackerInstall = firecrackerInstall;
        _bootWaitSeconds = bootWaitSeconds;
    }

    private async Task InternalStartAsync()
    {
        var configPath = await PrepareForBootAsync();
        var commandArgs = $" --config-file {configPath} --api-sock {_socketPath} --id {_vmId}";
        _process = _firecrackerInstall.RunFirecracker(commandArgs);
        
        Log.Information("Launched Firecracker microVM {vmId}", _vmId);
        
        if (_bootWaitSeconds.HasValue)
        {
            await Task.Delay(_bootWaitSeconds.Value * 1000);
            Log.Information("Waited {seconds} seconds for Firecracker microVM {vmId} to initialize",
                _bootWaitSeconds.Value, _vmId);
        }
    }

    private async Task<string> PrepareForBootAsync()
    {
        var configPath = Path.GetTempFileName() + ".json";
        var configJson = JsonSerializer.Serialize(_vmConfiguration, InternalUtil.SerializerOptions);
        await File.WriteAllTextAsync(configPath, configJson);
        
        Log.Debug("Configuration was serialized (to JSON) as a transit to: {configPath}", configPath);

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
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
        File.Delete(_socketPath);
        
        await _process!.StandardInput.WriteLineAsync("reboot");
        try
        {
            await _process.WaitForExitAsync(cancellationTokenSource.Token);
            Log.Information("Firecracker microVM {vmId} exited gracefully", _vmId);
        }
        catch (Exception)
        {
            _process.Kill();
            Log.Warning("Firecracker microVM {vmId} had to be forcefully killed", _vmId);
        }
    }
}