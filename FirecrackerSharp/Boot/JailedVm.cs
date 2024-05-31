using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

/// <summary>
/// A microVM that was booted through the jailer binary inside a chroot jail.
///
/// Refer to <see cref="UnrestrictedVm"/> documentation on when to use this instead of a <see cref="UnrestrictedVm"/>.
/// </summary>
public class JailedVm : Vm
{
    private static readonly ILogger Logger = Log.ForContext<JailedVm>();
    private readonly JailerOptions _jailerOptions;
    private readonly string _jailPath;
    private readonly string _socketPathInJail;

    private JailedVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        JailerOptions jailerOptions,
        string vmId) : base(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId)
    {
        _jailerOptions = jailerOptions;
        
        _jailPath = IHostFilesystem.Current.JoinPaths(_jailerOptions.ChrootBaseDirectory, "firecracker", vmId, "root");
        IHostFilesystem.Current.CreateDirectory(_jailPath);

        _socketPathInJail = IHostFilesystem.Current.JoinPaths(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        IHostFilesystem.Current.CreateDirectory(IHostFilesystem.Current.JoinPaths(_jailPath, firecrackerOptions.SocketDirectory));
        SocketPath = IHostFilesystem.Current.JoinPaths(_jailPath, _socketPathInJail);
    }

    internal override async Task StartProcessAsync()
    {
        VmConfiguration = await MoveAllToJailAsync(_jailPath);
        Logger.Debug("Moved all resources to jail of microVM {vmId}", VmId);

        string? internalConfigPath = null;
        if (VmConfiguration.ApplicationMode == VmConfigurationApplicationMode.ThroughJsonConfiguration)
        {
            internalConfigPath = "vm_config.json";
            var externalConfigPath = IHostFilesystem.Current.JoinPaths(_jailPath, "vm_config.json");
            await SerializeConfigToFileAsync(externalConfigPath);
        }

        var firecrackerArgs = FirecrackerOptions.FormatToArguments(internalConfigPath, _socketPathInJail);
        var jailerArgs = _jailerOptions.FormatToArguments(FirecrackerInstall.FirecrackerBinary, VmId);
        var args = $"{jailerArgs} -- {firecrackerArgs}";
        Logger.Debug("Launch arguments for microVM {vmId} (jailed) are: {args}", VmId, args);

        if (IHostProcessManager.Current.IsEscalated)
        {
            Process = IHostProcessManager.Current.LaunchProcess(FirecrackerInstall.JailerBinary, args);
        }
        else
        {
            if (_jailerOptions.SudoPassword is null)
                throw new ArgumentNullException(nameof(_jailerOptions.SudoPassword));
            
            Process = await IHostProcessManager.Current.EscalateAndLaunchProcessAsync(_jailerOptions.SudoPassword,
                FirecrackerInstall.JailerBinary, args);
        }

        await Task.Delay(TimeSpan.FromMilliseconds(_jailerOptions.WaitMillisAfterJailing));
        
        await HandlePostBootAsync();
        Logger.Information("Launched microVM {vmId} (jailed)", VmId);
    }

    protected override void CleanupAfterShutdown()
    {
        IHostFilesystem.Current.DeleteDirectoryRecursively(Directory.GetParent(_jailPath)!.FullName);
    }

    private async Task<VmConfiguration> MoveAllToJailAsync(string jailPath)
    {
        var tasks = new List<Task>();
        
        // move kernel
        tasks.Add(MoveToJailAsync(VmConfiguration.BootSource.KernelImagePath, jailPath, "kernel_image"));
        // move initrd if it's specified
        if (VmConfiguration.BootSource.InitrdPath != null)
        {
            tasks.Add(MoveToJailAsync(VmConfiguration.BootSource.InitrdPath, jailPath, "initrd"));
        }
        // move physical locations of drives
        var newDrives = new List<VmDrive>();
        foreach (var drive in VmConfiguration.Drives)
        {
            if (drive.PathOnHost != null)
            {
                var newPath = $"drive_{drive.DriveId}";
                tasks.Add(MoveToJailAsync(drive.PathOnHost, jailPath, newPath));
                newDrives.Add(drive with { PathOnHost = newPath });
            }
            else
            {
                newDrives.Add(drive);
            }
        }

        await Task.WhenAll(tasks);

        // recreate configuration with moved paths
        return VmConfiguration with
        {
            BootSource = VmConfiguration.BootSource with
            {
                KernelImagePath = "kernel_image",
                InitrdPath = VmConfiguration.BootSource.InitrdPath != null ? "initrd" : null
            },
            Drives = newDrives
        };
    }

    private static Task MoveToJailAsync(string originalPath, string jailPath, string newFilename)
    {
        return IHostFilesystem.Current.CopyFileAsync(
            originalPath, IHostFilesystem.Current.JoinPaths(jailPath, newFilename));
    }

    /// <summary>
    /// Boot up a <see cref="JailedVm"/> with the given parameters and return its instance for further management.
    /// </summary>
    /// <param name="vmConfiguration">The entire pre-boot <see cref="VmConfiguration"/> for this microVM</param>
    /// <param name="firecrackerInstall">The <see cref="FirecrackerInstall"/> to be used to boot this microVM</param>
    /// <param name="firecrackerOptions">The <see cref="FirecrackerOptions"/> to be passed into the firecracker binary</param>
    /// <param name="jailerOptions">The <see cref="JailerOptions"/> to be passed into the jailer binary</param>
    /// <param name="vmId">A unique microVM identifier that must not be repeated for multiple VMs.</param>
    /// <returns></returns>
    public static async Task<JailedVm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        JailerOptions jailerOptions,
        string vmId)
    {
        var vm = new JailedVm(vmConfiguration, firecrackerInstall, firecrackerOptions, jailerOptions, vmId);
        await vm.StartProcessAsync();
        return vm;
    }
}