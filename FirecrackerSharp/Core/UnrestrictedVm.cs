using FirecrackerSharp.Data;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Core;

/// <summary>
/// A <see cref="Vm"/> that was booted normally, as in not using the Firecracker jailer binary to create a chroot
/// jail.
///
/// Booting a <see cref="UnrestrictedVm"/> compared to a <see cref="JailedVm"/> has the upside of significantly better
/// boot time due to not having to create a chroot jail and manually copy all VM resources into that jail.
/// However, if security is a higher priority, a <see cref="JailedVm"/> is better with all the extra precautions.
///
/// We recommend opting for a <see cref="UnrestrictedVm"/> for all microVMs without special security concerns such as
/// having a connection to the outside Internet, while using <see cref="JailedVm"/> exclusively for those microVMs that
/// strictly require such isolation.
/// </summary>
public sealed class UnrestrictedVm : Vm
{
    private static readonly ILogger Logger = Log.ForContext<UnrestrictedVm>();

    /// <summary>
    /// Instantiate a new unrestricted microVM. After this, it'll be in the "not booted" lifecycle phase,
    /// use <see cref="Vm.BootAsync"/> to actually boot it. It is typical to also configure lifecycle logging before
    /// booting the VM.
    /// </summary>
    /// <param name="vmConfiguration">The <see cref="VmConfiguration"/> for this VM</param>
    /// <param name="firecrackerInstall">The <see cref="FirecrackerInstall"/> to be used</param>
    /// <param name="firecrackerOptions">The <see cref="FirecrackerOptions"/> to pass to the Firecracker binary</param>
    /// <param name="vmId">The unique identifier of this VM</param>
    public UnrestrictedVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId) : base(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId)
    {
        IHostFilesystem.Current.CreateDirectory(firecrackerOptions.SocketDirectory);
        SocketPath = IHostFilesystem.Current.JoinPaths(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        
        Logger.Debug("The Unix socket for the unrestricted microVM will be created at: {socketPath}", SocketPath);
    }

    protected override async Task BootInternalAsync()
    {
        string? configPath = null;
        if (VmConfiguration.ApplicationMode == VmConfigurationApplicationMode.JsonConfiguration)
        {
            configPath = IHostFilesystem.Current.GetTemporaryFilename();
            await SerializeConfigToFileAsync(configPath);
        }

        var args = FirecrackerOptions.FormatToArguments(configPath, SocketPath);
        Logger.Debug("Launch arguments for microVM {vmId} (unrestricted) are: {args}", VmId, args);
        Process = IHostProcessManager.Current.LaunchProcess(FirecrackerInstall.FirecrackerBinary, args);
    }

    protected override void CleanupAfterShutdown()
    {
        if (IHostFilesystem.Current.FileOrDirectoryExists(SocketPath!))
        {
            IHostFilesystem.Current.DeleteFile(SocketPath!);
        }
    }
}