using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Tests;

public static class VmArrangeUtility
{
    public static readonly VmConfiguration VmConfiguration = new(
        new VmBootSource("/opt/firecracker-sharp/vmlinux-5.10.217", "console=ttyS0 reboot=k panic=1 pci=off"),
        new VmMachineConfiguration(256, 1),
        Drives: [new VmDrive("rootfs", IsRootDevice: true, PathOnHost: "/opt/firecracker-sharp/ubuntu-22.04.ext4")]);

    public static FirecrackerOptions FirecrackerOptions => new(
        Guid.NewGuid().ToString());

    public static JailerOptions JailerOptions => new(
        1000, 1000, SudoPassword: "495762");

    public static readonly FirecrackerInstall FirecrackerInstall = new(
        "v1.7.0",
        "/opt/firecracker-sharp/firecracker",
        "/opt/firecracker-sharp/jailer");
    
    public static string VmId => Random.Shared.NextInt64(100000).ToString();

    public static async Task<Vm> StartUnrestrictedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.ThroughApiCalls)
    {
        return await UnrestrictedVm.StartAsync(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            VmId);
    }

    public static async Task<Vm> StartJailedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.ThroughApiCalls)
    {
        return await JailedVm.StartAsync(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            JailerOptions,
            VmId);
    }
}