using System.Diagnostics;
using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Core;

public class FirecrackerVm
{
    private static readonly ILogger Logger = Log.ForContext(typeof(FirecrackerVm));
    
    private readonly VmConfiguration _vmConfiguration;
    private readonly FirecrackerInstall _firecrackerInstall;
    private readonly FirecrackerOptions _firecrackerOptions;
    private readonly string _socketPath;
    
    private Process? _process;
    private readonly Guid _vmId = Guid.NewGuid();

    private FirecrackerVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions)
    {
        if (!Directory.Exists(firecrackerOptions.SocketDirectory)) Directory.CreateDirectory(firecrackerOptions.SocketDirectory);
        _socketPath = Path.Join(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        
        Logger.Debug("The Unix socket for the microVM will be created at: {socketPath}", _socketPath);
        
        _vmConfiguration = vmConfiguration;
        _firecrackerInstall = firecrackerInstall;
        _firecrackerOptions = firecrackerOptions;
    }

    private async Task InternalStartAsync()
    {
        var configPath = await PrepareForBootAsync();
        var args = $"--config-file {configPath} --api-sock {_socketPath} {_firecrackerOptions.ExtraArguments}";
        Log.Debug("Launch arguments for microVM {vmId} are: {args}", _vmId, args);
        _process = _firecrackerInstall.RunFirecracker(args);
        
        if (_firecrackerOptions.WaitSecondsAfterBoot.HasValue)
        {
            await Task.Delay(_firecrackerOptions.WaitSecondsAfterBoot.Value * 1000);
        }
        Log.Information("Launched microVM {vmId}", _vmId);
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
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions)
    {
        var firecrackerVm = new FirecrackerVm(vmConfiguration, firecrackerInstall, firecrackerOptions);
        await firecrackerVm.InternalStartAsync();
        return firecrackerVm;
    }

    public async Task ShutdownAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
        File.Delete(_socketPath);
        
        await _process!.StandardInput.WriteLineAsync("reboot");
        try
        {
            await _process.WaitForExitAsync(cancellationTokenSource.Token);
            Log.Information("microVM {vmId} exited gracefully", _vmId);
        }
        catch (Exception)
        {
            _process.Kill();
            Log.Warning("microVM {vmId} had to be forcefully killed", _vmId);
        }
    }
}