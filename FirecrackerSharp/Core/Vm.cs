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
    /// This is a running microVM facility, meaning it is only available during the active <see cref="VmLifecyclePhase"/>,
    /// otherwise a <see cref="NotAccessibleDueToLifecycleException"/> will be thrown during a get attempt.
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
    /// This is a running microVM facility, meaning it is only available during the active <see cref="VmLifecyclePhase"/>,
    /// otherwise a <see cref="NotAccessibleDueToLifecycleException"/> will be thrown during a get attempt.
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

    /// <summary>
    /// The <see cref="VmLifecycle"/> instance linked to this microVM and used for monitoring its lifecycle and managing
    /// log targets.
    /// This is not a running microVM facility, so it is available during all <see cref="VmLifecyclePhase"/>s.
    /// </summary>
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

    /// <summary>
    /// Boot the microVM.
    /// </summary>
    /// <exception cref="NotAccessibleDueToLifecycleException">If this was called not during the "not booted"
    /// phase, which is the only one supported for this operation</exception>
    public async Task BootAsync()
    {
        if (Lifecycle.CurrentPhase != VmLifecyclePhase.NotBooted)
        {
            throw new NotAccessibleDueToLifecycleException("A microVM can only be booted once");
        }

        Lifecycle.CurrentPhase = VmLifecyclePhase.Booting;
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
            if (VmConfiguration.WaitOptions.DelayBeforeBootApiRequests.HasValue)
            {
                await Task.Delay(VmConfiguration.WaitOptions.DelayBeforeBootApiRequests.Value);
            }

            var cancellationToken = new CancellationTokenSource(VmConfiguration.WaitOptions.TimeoutForBootApiRequests).Token;
            await _management.ApplyVmConfigurationAsync(
                parallelize: VmConfiguration.ApplicationMode == VmConfigurationApplicationMode.ParallelizedApiCalls,
                cancellationToken);
            
            await _management.PerformActionAsync(new VmAction(VmActionType.InstanceStart), cancellationToken);
        }

        if (VmConfiguration.WaitOptions.DelayForBoot.HasValue)
        {
            await Task.Delay(VmConfiguration.WaitOptions.DelayForBoot.Value);
        }

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
                await _ttyClient.BeginPrimaryWriteAsync(
                    VmConfiguration.TtyAuthentication.Username,
                    cancellationToken: ttyAuthenticationTokenSource.Token);
                _ttyClient.CompletePrimaryWrite();
            }

            await _ttyClient.BeginPrimaryWriteAsync(
                VmConfiguration.TtyAuthentication.Password,
                cancellationToken: ttyAuthenticationTokenSource.Token);
            _ttyClient.CompletePrimaryWrite();
        }
        catch (TtyException)
        {
            Logger.Error("TTY authentication failed for microVM {vmId}, likely due to a" +
                         " misconfiguration. A graceful shutdown may not be possible", VmId);
        }
    }

    /// <summary>
    /// Shut down this microVM and dispose of all associated transient resources.
    /// </summary>
    /// <exception cref="NotAccessibleDueToLifecycleException">If this was called not during the active lifecycle phase,
    /// which is the only one supported</exception>
    /// <returns>The <see cref="VmShutdownResult"/> representing the outcome of this shutdown attempt</returns>
    public async Task<VmShutdownResult> ShutdownAsync()
    {
        if (Lifecycle.IsNotActive)
        {
            throw new NotAccessibleDueToLifecycleException("Cannot shutdown microVM when it hasn't booted up yet");
        }

        Lifecycle.CurrentPhase = VmLifecyclePhase.ShuttingDown;
        
        if (_backingSocket != null)
        {
            Socket.Dispose();
        }

        var shutdownResult = VmShutdownResult.Successful;
        var cancellationToken = new CancellationTokenSource(VmConfiguration.WaitOptions.TimeoutForShutdown).Token;

        try
        {
            await Process!.WriteLineAsync("reboot", cancellationToken);
            var exited = await Process!.WaitForExitAsync(TimeSpan.FromSeconds(30), "Firecracker exiting");
            if (exited)
            {
                Logger.Information("microVM {vmId} exited gracefully", VmId);
            }
            else
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
        catch (Exception exception)
        {
            Logger.Warning("microVM {vmId} wasn't shut down due to an unknown exception: {exception}", VmId, exception);
            shutdownResult = VmShutdownResult.FailedDueToUnknownReason;
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
        Lifecycle.CurrentPhase = VmLifecyclePhase.PoweredOff;

        return shutdownResult;
    }
}