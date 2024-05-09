using System.Diagnostics;
using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

public abstract class FirecrackerVm
{
    private static readonly ILogger Logger = Log.ForContext(typeof(FirecrackerVm));
    
    protected VmConfiguration VmConfiguration;
    protected readonly FirecrackerInstall FirecrackerInstall;
    protected readonly FirecrackerOptions FirecrackerOptions;
    protected string SocketPath;
    
    protected Process? Process;
    protected readonly Guid VmId = Guid.NewGuid();

    protected FirecrackerVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions)
    {
        if (!Directory.Exists(firecrackerOptions.SocketDirectory)) Directory.CreateDirectory(firecrackerOptions.SocketDirectory);
        SocketPath = Path.Join(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        
        Logger.Debug("The Unix socket for the microVM will be created at: {socketPath}", SocketPath);
        
        VmConfiguration = vmConfiguration;
        FirecrackerInstall = firecrackerInstall;
        FirecrackerOptions = firecrackerOptions;
    }

    internal abstract Task StartProcessAsync();

    protected async Task SerializeConfigToFileAsync(string configPath)
    {
        var configJson = JsonSerializer.Serialize(VmConfiguration, InternalUtil.SerializerOptions);
        await File.WriteAllTextAsync(configPath, configJson);
        
        Log.Debug("Configuration was serialized (to JSON) as a transit to: {configPath}", configPath);
    }

    protected async Task WaitForBootAsync()
    {
        if (FirecrackerOptions.WaitSecondsAfterBoot.HasValue)
        {
            await Task.Delay(FirecrackerOptions.WaitSecondsAfterBoot.Value * 1000);
        }
    }

    public async Task ShutdownAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
        File.Delete(SocketPath);
        
        await Process!.StandardInput.WriteLineAsync("reboot");
        try
        {
            await Process.WaitForExitAsync(cancellationTokenSource.Token);
            Log.Information("microVM {vmId} exited gracefully", VmId);
        }
        catch (Exception)
        {
            Process.Kill();
            Log.Warning("microVM {vmId} had to be forcefully killed", VmId);
        }
    }
}