using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Management;
using FirecrackerSharp.Terminal;
using Serilog;

namespace FirecrackerSharp.Boot;

/// <summary>
/// The base class representing all Firecracker microVMs.
/// </summary>
public abstract class Vm
{
    private static readonly ILogger Logger = Log.ForContext<Vm>();
    
    protected readonly FirecrackerInstall FirecrackerInstall;
    protected readonly FirecrackerOptions FirecrackerOptions;
    protected string? SocketPath;
    protected internal readonly string VmId;

    protected internal VmConfiguration VmConfiguration;
    protected internal IHostProcess? Process;

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

    /// <summary>
    /// The <see cref="VmTerminal"/> instance linked to this <see cref="Vm"/> that allows one-operation-per-time
    /// access to the VM's main TTY if one is started upon boot.
    /// </summary>
    public readonly VmTerminal Terminal;

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
        Terminal = new VmTerminal(this);
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
        var source = new CancellationTokenSource(TimeSpan.FromSeconds(FirecrackerOptions.WaitSecondsAfterBoot ?? 5));
        await Terminal.LogLifecycleAsync(source);
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
        
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(30));

        await Terminal.LogLifecycleAsync(source);
        
        try
        {
            await Process!.StandardInput.WriteAsync(
                new ReadOnlyMemory<byte>("reboot\n"u8.ToArray()), source.Token);
            try
            {
                await Process.WaitUntilCompletionAsync(source.Token);
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