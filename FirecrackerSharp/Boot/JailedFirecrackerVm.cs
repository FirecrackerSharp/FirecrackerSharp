using FirecrackerSharp.Data;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

public class JailedFirecrackerVm : FirecrackerVm
{
    private static readonly ILogger Logger = Log.ForContext<JailedFirecrackerVm>();
    private readonly JailerOptions _jailerOptions;
    private readonly string _jailPath;
    private readonly string _socketPathInJail;

    private JailedFirecrackerVm(
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
        // create and move all to jail
        VmConfiguration = await MoveAllToJailAsync(_jailPath);
        Logger.Debug("Moved all resources to jail of microVM {vmId}", VmId);
        // move config
        var configPath = IHostFilesystem.Current.JoinPaths(_jailPath, "vm_config.json");
        await SerializeConfigToFileAsync(configPath);
        
        var firecrackerArgs = FirecrackerOptions.FormatToArguments("vm_config.json", _socketPathInJail);
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

        await WaitForBootAsync();
        Logger.Information("Launched microVM {vmId} (jailed)", VmId);
    }

    protected override void CleanupAfterShutdown()
    {
        IHostFilesystem.Current.DeleteDirectoryRecursively(_jailPath);
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

    public static async Task<FirecrackerVm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        JailerOptions jailerOptions,
        string vmId)
    {
        var vm = new JailedFirecrackerVm(vmConfiguration, firecrackerInstall, firecrackerOptions, jailerOptions, vmId);
        await vm.StartProcessAsync();
        return vm;
    }
}