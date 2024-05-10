using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Transport;
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
        
        _jailPath = Path.Join(_jailerOptions.ChrootBaseDirectory, "firecracker", vmId, "root");
        IFirecrackerTransport.Current.CreateDirectory(_jailPath);

        _socketPathInJail = Path.Join(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        IFirecrackerTransport.Current.CreateDirectory(Path.Join(_jailPath, firecrackerOptions.SocketDirectory));
        SocketPath = Path.Join(_jailPath, _socketPathInJail);
    }

    internal override async Task StartProcessAsync()
    {
        // create and move all to jail
        VmConfiguration = await MoveAllToJailAsync(_jailPath);
        Logger.Debug("Moved all resources to jail of microVM {vmId}", VmId);
        // move config
        var configPath = Path.Join(_jailPath, "vm_config.json");
        await SerializeConfigToFileAsync(configPath);
        
        var firecrackerArgs = FirecrackerOptions.FormatToArguments("vm_config.json", _socketPathInJail);
        var jailerArgs = _jailerOptions.FormatToArguments(FirecrackerInstall.FirecrackerBinary, VmId);
        var args = $"{jailerArgs} -- {firecrackerArgs}";
        Logger.Debug("Launch arguments for microVM {vmId} (jailed) are: {args}", VmId, args);
        Process = IFirecrackerTransport.Current.LaunchProcess(FirecrackerInstall.JailerBinary, args);

        await WaitForBootAsync();
        Logger.Information("Launched microVM {vmId} (jailed)", VmId);
    }

    public override void CleanupAfterShutdown()
    {
        Directory.Delete(_jailPath, recursive: true);
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
        return IFirecrackerTransport.Current.CopyFileAsync(
            originalPath, Path.Join(jailPath, newFilename));
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