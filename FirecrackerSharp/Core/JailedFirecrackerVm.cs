using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Core;

public class JailedFirecrackerVm : FirecrackerVm
{
    private readonly JailerOptions _jailerOptions;

    private JailedFirecrackerVm(VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        JailerOptions jailerOptions) : base(vmConfiguration, firecrackerInstall, firecrackerOptions)
    {
        _jailerOptions = jailerOptions;
    }

    internal override async Task StartProcessAsync()
    {
        var jailPath = Path.Join(
            _jailerOptions.ChrootBaseDirectory, "firecracker", VmId.ToString(), "root");
        Directory.CreateDirectory(jailPath);
        VmConfiguration = await MoveAllToJailAsync(jailPath);
        Console.WriteLine("neat");
    }

    private async Task<VmConfiguration> MoveAllToJailAsync(string jailPath)
    {
        var tasks = new List<Task>();
        
        tasks.Add(MoveToJailAsync(VmConfiguration.BootSource.KernelImagePath, jailPath, "kernel_image"));
        if (VmConfiguration.BootSource.InitrdPath != null)
        {
            tasks.Add(MoveToJailAsync(VmConfiguration.BootSource.InitrdPath, jailPath, "initrd"));
        }

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

        return VmConfiguration with
        {
            BootSource = VmConfiguration.BootSource with
            {
                KernelImagePath = "kernelImage",
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