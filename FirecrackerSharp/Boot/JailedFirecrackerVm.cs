using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

public class JailedFirecrackerVm : FirecrackerVm
{
    private static readonly ILogger Logger = Log.ForContext(typeof(JailedFirecrackerVm));
    private readonly JailerOptions _jailerOptions;
    private readonly string _jailPath;

    private JailedFirecrackerVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        JailerOptions jailerOptions) : base(vmConfiguration, firecrackerInstall, firecrackerOptions)
    {
        _jailerOptions = jailerOptions;
        
        _jailPath = Path.Join(_jailerOptions.ChrootBaseDirectory, "firecracker", _jailerOptions.JailId, "root");
        Directory.CreateDirectory(_jailPath);
        
        SocketPath = Path.Join(_jailPath, "run", "firecracker.socket");
    }

    internal override async Task StartProcessAsync()
    {
        // create and move all to jail
        VmConfiguration = await MoveAllToJailAsync(_jailPath);
        Logger.Debug("Moved all resources to jail {jailId} of microVM {vmId}", _jailerOptions.JailId, VmId);
        // move config
        var configPath = Path.Join(_jailPath, "vm_config.json");
        await SerializeConfigToFileAsync(configPath);
        
        var firecrackerArgs = FirecrackerOptions.FormatToArguments("vm_config.json", null);
        var jailerArgs = _jailerOptions.FormatToArguments(FirecrackerInstall.FirecrackerBinary);
        var args = $"{jailerArgs} -- {firecrackerArgs}";
        Logger.Debug("Launch arguments for microVM {vmId} (jail {jailId}) are: {args}",
            VmId, _jailerOptions.JailId, args);
        Process = await InternalUtil.RunProcessInSudoAsync(_jailerOptions.SudoPassword, FirecrackerInstall.JailerBinary, args);

        await WaitForBootAsync();
        Logger.Information("Launched microVM {vmId} (jail {jailId})", VmId, _jailerOptions.JailId);
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

    private static async Task MoveToJailAsync(string originalPath, string jailPath, string newFilename)
    {
        var newPath = Path.Join(jailPath, newFilename);
        await using var sourceStream = File.OpenRead(originalPath);
        await using var destinationStream = File.OpenWrite(newPath);
        await sourceStream.CopyToAsync(destinationStream);
    }

    public static async Task<FirecrackerVm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        JailerOptions jailerOptions)
    {
        var vm = new JailedFirecrackerVm(vmConfiguration, firecrackerInstall, firecrackerOptions, jailerOptions);
        await vm.StartProcessAsync();
        return vm;
    }
}