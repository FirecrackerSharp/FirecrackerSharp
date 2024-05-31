using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Ballooning;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Tests.Helpers;

public static class VmArrange
{
    private static readonly VmConfiguration VmConfiguration = new(
        new VmBootSource("/opt/firecracker-sharp/vmlinux-5.10.217", "console=ttyS0 reboot=k panic=1 pci=off"),
        new VmMachineConfiguration(256, 1),
        Drives: [new VmDrive("rootfs", IsRootDevice: true, PathOnHost: "/opt/firecracker-sharp/ubuntu-22.04.ext4")],
        Balloon: new VmBalloon(AmountMib: 128, DeflateOnOom: false, StatsPollingIntervalS: 1));

    private static FirecrackerOptions FirecrackerOptions => new(
        Guid.NewGuid().ToString());

    private static JailerOptions JailerOptions => new(
        1000, 1000, SudoPassword: Environment.GetEnvironmentVariable("FSH_ROOT_PASSWORD"));

    private static readonly FirecrackerInstall FirecrackerInstall = new(
        "v1.7.0",
        "/opt/firecracker-sharp/firecracker",
        "/opt/firecracker-sharp/jailer");

    private static string VmId => Random.Shared.NextInt64(100000).ToString();

    public static async Task<Vm> StartUnrestrictedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.ThroughJsonConfiguration)
    {
        return await UnrestrictedVm.StartAsync(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            VmId);
    }

    public static async Task<Vm> StartJailedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.ThroughJsonConfiguration)
    {
        return await JailedVm.StartAsync(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            JailerOptions,
            VmId);
    }
}