using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Core;

/// <summary>
/// A microVM that was booted through the jailer binary inside a chroot jail.
///
/// Refer to <see cref="UnrestrictedVm"/> documentation on when to use this instead of a <see cref="UnrestrictedVm"/>.
/// </summary>
public sealed class JailedVm : Vm
{
    private static readonly ILogger Logger = Log.ForContext<JailedVm>();
    private readonly JailerOptions _jailerOptions;
    private readonly string _jailPath;
    private readonly string _socketPathInJail;

    /// <summary>
    /// Instantiate a new jailed microVM. After this, it'll be in the "not booted" lifecycle phase,
    /// use <see cref="Vm.BootAsync"/> to actually boot it. It is typical to also configure lifecycle logging before
    /// booting the VM.
    /// </summary>
    /// <param name="vmConfiguration">The <see cref="VmConfiguration"/> for this VM</param>
    /// <param name="firecrackerInstall">The <see cref="FirecrackerInstall"/> whose binaries to use</param>
    /// <param name="firecrackerOptions">The <see cref="FirecrackerOptions"/> to be passed to the firecracker binary</param>
    /// <param name="jailerOptions">The <see cref="JailerOptions"/> to be passed to the jailer binary</param>
    /// <param name="vmId">The unique identifier of this VM</param>
    public JailedVm(
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

    protected override async Task BootInternalAsync()
    {
        VmConfiguration = await MoveAllToJailAsync(_jailPath);
        Logger.Debug("Moved all resources to jail of microVM {vmId}", VmId);

        string? internalConfigPath = null;
        if (VmConfiguration.ApplicationMode == VmConfigurationApplicationMode.JsonConfiguration)
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
    }

    protected override void CleanupAfterShutdown()
    {
        var jailParentDirectory = Directory.GetParent(_jailPath)!.FullName;
        IHostFilesystem.Current.DeleteDirectoryRecursively(jailParentDirectory);
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
}