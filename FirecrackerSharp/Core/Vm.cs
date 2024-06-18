using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Actions;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Management;
using FirecrackerSharp.Tty;
using Serilog;

namespace FirecrackerSharp.Core;

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

    private readonly VmManagement _management;
    private readonly VmTtyClient _ttyClient;

    /// <summary>
    /// The <see cref="VmManagement"/> instance linked to this <see cref="Vm"/> that allows access to the Firecracker
    /// Management API that is linked to this <see cref="Vm"/>'s Firecracker UDS.
    /// </summary>
    public VmManagement Management
    {
        get
        {
            if (Lifecycle.IsNotActive)
                throw new NotAccessibleDueToLifecycleException("A microVM cannot be managed when not active");
            return _management;
        }
    }

    /// <summary>
    /// The <see cref="VmTtyClient"/> instance linked to this <see cref="Vm"/> that allows direct access to the microVM's
    /// serial console / boot TTY.
    /// </summary>
    public VmTtyClient TtyClient
    {
        get
        {
            if (Lifecycle.IsNotActive)
                throw new NotAccessibleDueToLifecycleException("A microVM's TTY cannot be accessed when not active");
            return _ttyClient;
        }
    }

    public readonly VmLifecycle Lifecycle = new();

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
        _management = new VmManagement(this);
        _ttyClient = new VmTtyClient(this);
    }

    public async Task BootAsync()
    {
        if (Lifecycle.CurrentPhase != VmLifecyclePhase.PreBoot)
        {
            throw new NotAccessibleDueToLifecycleException("A microVM can only be booted once");
        }

        Lifecycle.CurrentPhase = VmLifecyclePhase.Boot;
        await BootInternalAsync();
        await HandlePostBootAsync();
        Logger.Information("Launched microVM {vmId}", VmId);
        Lifecycle.CurrentPhase = VmLifecyclePhase.Active;
    }

    protected abstract Task BootInternalAsync();
    protected abstract void CleanupAfterShutdown();

    protected async Task SerializeConfigToFileAsync(string configPath)
    {
        var configJson = JsonSerializer.Serialize(VmConfiguration, FirecrackerSerialization.Options);
        await IHostFilesystem.Current.WriteTextFileAsync(configPath, configJson);
        
        Logger.Debug("Configuration was serialized (to JSON) as a transit to: {configPath}", configPath);
    }

    private async Task HandlePostBootAsync()
    {
        _ttyClient.StartListening();
        
        if (VmConfiguration.ApplicationMode != VmConfigurationApplicationMode.JsonConfiguration)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(FirecrackerOptions.WaitMillisForSocketInitialization));
            
            await _management.ApplyVmConfigurationAsync(
                parallelize: VmConfiguration.ApplicationMode == VmConfigurationApplicationMode.ParallelizedApiCalls);
            
            await _management.PerformActionAsync(new VmAction(VmActionType.InstanceStart));
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
                await _ttyClient.WriteAsync(
                    VmConfiguration.TtyAuthentication.Username,
                    cancellationToken: ttyAuthenticationTokenSource.Token);
            }

            await _ttyClient.WriteAsync(
                VmConfiguration.TtyAuthentication.Password,
                cancellationToken: ttyAuthenticationTokenSource.Token);
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
    public async Task<VmShutdownResult> ShutdownAsync()
    {
        if (Lifecycle.IsNotActive)
        {
            throw new NotAccessibleDueToLifecycleException("Cannot shutdown microVM when it hasn't booted up yet");
        }

        Lifecycle.CurrentPhase = VmLifecyclePhase.Shutdown;
        
        if (_backingSocket != null)
        {
            Socket.Dispose();
        }

        var shutdownResult = VmShutdownResult.Successful;
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await _ttyClient.WriteAsync("reboot", cancellationToken: cancellationTokenSource.Token);
            try
            {
                await Process!.WaitForGracefulExitAsync(TimeSpan.FromSeconds(30));
                Logger.Information("microVM {vmId} exited gracefully", VmId);
            }
            catch (Exception)
            {
                await Process!.KillAsync();
                Logger.Warning("microVM {vmId} had to be forcefully killed", VmId);
                shutdownResult = VmShutdownResult.FailedDueToHangingProcess;
            }
        }
        catch (IOException)
        {
            Logger.Warning("microVM {vmId} prematurely shut down", VmId);
            shutdownResult = VmShutdownResult.FailedDueToBrokenPipe;
        }
        catch (TtyException)
        {
            Logger.Warning("microVM {vmId} didn't respond to reboot signal being written to TTY", VmId);
            shutdownResult = VmShutdownResult.FailedDueToTtyNotResponding;
        }

        try
        {
            CleanupAfterShutdown();
        }
        catch (Exception)
        {
            shutdownResult = VmShutdownResult.SoftFailedDuringCleanup;
        }

        Log.Information("microVM {vmId} was successfully cleaned up after shutdown (socket/jail deleted)", VmId);
        Lifecycle.FinishLastPhase();

        return shutdownResult;
    }
}