using System.Text;
using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Actions;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Management;
using FirecrackerSharp.Tty;
using Serilog;

namespace FirecrackerSharp.Boot;

/// <summary>
/// The base class representing all Firecracker microVMs.
/// </summary>
public abstract class Vm
{
    private static readonly ILogger Logger = Log.ForContext<Vm>();
    
    protected internal VmConfiguration VmConfiguration;
    protected readonly FirecrackerInstall FirecrackerInstall;
    protected readonly FirecrackerOptions FirecrackerOptions;
    protected string? SocketPath;
    protected internal readonly string VmId;

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
    /// The <see cref="VmTtyManager"/> instance linked to this <see cref="Vm"/> that allows semi-parallel (although
    /// limited, please refer to <see cref="VmTtyManager"/> documentation) access to the kernel TTY for executing
    /// commands without a networking setup.
    /// </summary>
    public readonly VmTtyManager TtyManager;

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
        TtyManager = new VmTtyManager(this);
    }

    internal abstract Task StartProcessAsync();

    protected abstract void CleanupAfterShutdown();

    protected async Task SerializeConfigToFileAsync(string configPath)
    {
        var configJson = JsonSerializer.Serialize(VmConfiguration, FirecrackerSerialization.Options);
        await IHostFilesystem.Current.WriteTextFileAsync(configPath, configJson);
        
        Logger.Debug("Configuration was serialized (to JSON) as a transit to: {configPath}", configPath);
    }

    protected async Task HandlePostBootAsync()
    {
        if (VmConfiguration.ApplicationMode != VmConfigurationApplicationMode.JsonConfiguration)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(FirecrackerOptions.WaitMillisForSocketInitialization));
            
            await Management.ApplyVmConfigurationAsync(
                parallelize: VmConfiguration.ApplicationMode == VmConfigurationApplicationMode.ParallelizedApiCalls);
            
            await Management.PerformActionAsync(new VmAction(VmActionType.InstanceStart));
        }
        
        await Task.Delay(TimeSpan.FromMilliseconds(FirecrackerOptions.WaitMillisAfterBoot));
        
        await AuthenticateTtyAsync();
    }

    private async Task AuthenticateTtyAsync()
    {
        if (VmConfiguration.TtyAuthentication is null) return;
        
        try
        {
            var ttyAuthenticationTokenSource = new CancellationTokenSource(
                TimeSpan.FromSeconds(VmConfiguration.TtyAuthentication.TimeoutSeconds));

            if (!VmConfiguration.TtyAuthentication.UsernameAutofilled)
            {
                // await TtyManager.WriteToTtyAsync(
                //     VmConfiguration.TtyAuthentication.Username,
                //     ttyAuthenticationTokenSource.Token,
                //     subsequentlyRead: false);
            }

            // await TtyManager.WriteToTtyAsync(
            //     VmConfiguration.TtyAuthentication.Password,
            //     ttyAuthenticationTokenSource.Token);
        }
        catch (TtyException)
        {
            Logger.Error("TTY authentication failed for microVM {vmId}, likely due to a" +
                         " misconfiguration. A graceful shutdown may not be possible", VmId);
        }
    }

    /// <summary>
    /// Shutdown this microVM and dispose of all associated transient resources.
    ///
    /// The shutdown process can either succeed after a graceful shutdown, or fail if the microVM process had been
    /// killed before <see cref="ShutdownAsync"/> was called or if the microVM didn't respond to the TTY exit command
    /// ("reboot") and thus had the process was terminated.
    /// </summary>
    /// <returns>Whether the shutdown was graceful</returns>
    public async Task<bool> ShutdownAsync()
    {
        if (_backingSocket != null)
        {
            Socket.Dispose();
        }

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
        var gracefulShutdown = false;
        
        try
        {
            await Process!.StdinWriter.WriteLineAsync(new StringBuilder("reboot"), cancellationTokenSource.Token);
            try
            {
                await Process.WaitForGracefulExitAsync(TimeSpan.FromSeconds(30));
                Logger.Information("microVM {vmId} exited gracefully", VmId);
                gracefulShutdown = true;
            }
            catch (Exception)
            {
                await Process.KillAsync();
                Logger.Warning("microVM {vmId} had to be forcefully killed", VmId);
            }
        }
        catch (Exception)
        {
            Logger.Warning("microVM {vmId} prematurely shut down", VmId);
        }
        
        CleanupAfterShutdown();
        Log.Information("microVM {vmId} was successfully cleaned up after shutdown (socket/jail deleted)", VmId);

        return gracefulShutdown;
    }
}