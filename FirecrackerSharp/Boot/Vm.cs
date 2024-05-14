using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Management;
using Serilog;

namespace FirecrackerSharp.Boot;

/// <summary>
/// The base class representing all Firecracker microVMs.
/// </summary>
public abstract class Vm
{
    private static readonly ILogger Logger = Log.ForContext<Vm>();
    
    protected VmConfiguration VmConfiguration;
    protected readonly FirecrackerInstall FirecrackerInstall;
    protected readonly FirecrackerOptions FirecrackerOptions;
    protected string? SocketPath;
    protected readonly string VmId;

    protected IHostProcess? Process;

    private IHostSocket? _backingSocket;
    internal IHostSocket Socket
    {
        get
        {
            _backingSocket ??= IHostSocketManager.Current.Connect(SocketPath!, "http://localhost:80");
            return _backingSocket;
        }
    }
    
    /// <summary>
    /// The <see cref="VmManagement"/> instance linked to this <see cref="Vm"/> that allows access to the Firecracker
    /// Management API that is linked to this <see cref="Vm"/>'s Firecracker UDS.
    /// </summary>
    public readonly VmManagement Management;

    protected Vm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId)
    {
        VmConfiguration = vmConfiguration;
        FirecrackerInstall = firecrackerInstall;
        FirecrackerOptions = firecrackerOptions;
        VmId = vmId;
        Management = new VmManagement(this);
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

    /// <summary>
    /// Shutdown this microVM and dispose of all associated transient resources.
    ///
    /// The shutdown process can either succeed after a graceful shutdown, or fail if the microVM process had been
    /// killed before <see cref="ShutdownAsync"/> was called or if the microVM didn't respond to the TTY exit command
    /// ("reboot") and thus had the process was terminated.
    /// </summary>
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