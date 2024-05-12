using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

public abstract class FirecrackerVm(
    VmConfiguration vmConfiguration,
    FirecrackerInstall firecrackerInstall,
    FirecrackerOptions firecrackerOptions,
    string vmId)
{
    private static readonly ILogger Logger = Log.ForContext<FirecrackerVm>();
    
    protected VmConfiguration VmConfiguration = vmConfiguration;
    protected readonly FirecrackerInstall FirecrackerInstall = firecrackerInstall;
    protected readonly FirecrackerOptions FirecrackerOptions = firecrackerOptions;
    protected string? SocketPath;
    protected readonly string VmId = vmId;

    protected IHostProcess? Process;

    private IHostSocket? _backingSocket;
    public IHostSocket Socket
    {
        get
        {
            _backingSocket ??= IHostSocketManager.Current.Connect(SocketPath!, "http://localhost:80");
            return _backingSocket;
        }
    }

    internal abstract Task StartProcessAsync();

    protected abstract void CleanupAfterShutdown();

    protected async Task SerializeConfigToFileAsync(string configPath)
    {
        var configJson = JsonSerializer.Serialize(VmConfiguration, FirecrackerSerialization.Options);
        await IHostFilesystem.Current.WriteTextFileAsync(configPath, configJson);
        
        Logger.Debug("Configuration was serialized (to JSON) as a transit to: {configPath}", configPath);
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
        Socket.Dispose();
        
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
        
        try
        {
            await Process!.StandardInput.WriteAsync(
                new ReadOnlyMemory<byte>("reboot\n"u8.ToArray()), cancellationTokenSource.Token);
            try
            {
                await Process.WaitUntilCompletionAsync(cancellationTokenSource.Token);
                Logger.Information("microVM {vmId} exited gracefully", VmId);
            }
            catch (Exception)
            {
                Process.Kill();
                Logger.Warning("microVM {vmId} had to be forcefully killed", VmId);
            }
        }
        catch (Exception)
        {
            Logger.Warning("microVM {vmId} prematurely shut down", VmId);
        }
        
        CleanupAfterShutdown();
        Log.Information("microVM {vmId} was successfully cleaned up after shutdown (socket/jail deleted)", VmId);
    }
}