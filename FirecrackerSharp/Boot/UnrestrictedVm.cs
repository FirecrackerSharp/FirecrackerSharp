using FirecrackerSharp.Data;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

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
public class UnrestrictedVm : Vm
{
    private static readonly ILogger Logger = Log.ForContext<UnrestrictedVm>();

    private UnrestrictedVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId) : base(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId)
    {
        IHostFilesystem.Current.CreateDirectory(firecrackerOptions.SocketDirectory);
        SocketPath = IHostFilesystem.Current.JoinPaths(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        
        Logger.Debug("The Unix socket for the unrestricted microVM will be created at: {socketPath}", SocketPath);
    }
    
    internal override async Task StartProcessAsync()
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

        await HandlePostBootAsync();
        Logger.Information("Launched microVM {vmId} (unrestricted)", VmId);
    }

    protected override void CleanupAfterShutdown()
    {
        IHostFilesystem.Current.DeleteFile(SocketPath!);
    }

    /// <summary>
    /// Boot up a <see cref="UnrestrictedVm"/> with the given parameters and return its instance for further management.
    /// </summary>
    /// <param name="vmConfiguration">The entire pre-boot <see cref="VmConfiguration"/> for this microVM</param>
    /// <param name="firecrackerInstall">The <see cref="FirecrackerInstall"/> to be used to boot this microVM</param>
    /// <param name="firecrackerOptions">The <see cref="FirecrackerOptions"/> to be passed into the firecracker binary</param>
    /// <param name="vmId">A unique microVM identifier that must not be repeated for multiple VMs. A <see cref="Guid"/>
    /// can't be used due to length restrictions imposed by Firecracker (up to 50 characters)!</param>
    /// <returns>The booted <see cref="Vm"/></returns>
    public static async Task<Vm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId)
    {
        var vm = new UnrestrictedVm(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId);
        await vm.StartProcessAsync();
        return vm;
    }
}