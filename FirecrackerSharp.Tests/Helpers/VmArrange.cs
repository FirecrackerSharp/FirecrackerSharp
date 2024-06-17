using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Ballooning;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Tests.Helpers;

public static class VmArrange
{
    private static readonly VmConfiguration VmConfiguration = new(
        new VmBootSource("/opt/testdata/vmlinux-5.10.217", "console=ttyS0 reboot=k panic=1 pci=off"),
        new VmMachineConfiguration(256, 1),
        Drives: [new VmDrive("rootfs", IsRootDevice: true, PathOnHost: "/opt/testdata/ubuntu-22.04.ext4")],
        Balloon: new VmBalloon(AmountMib: 128, DeflateOnOom: false, StatsPollingIntervalS: 1));

    private static FirecrackerOptions FirecrackerOptions => new(
        Guid.NewGuid().ToString(),
        WaitMillisAfterBoot: 2000,
        WaitMillisForSocketInitialization: 200);

    private static JailerOptions JailerOptions => new(
        1000, 1000, SudoPassword: Environment.GetEnvironmentVariable("FSH_ROOT_PASSWORD"));

    private static readonly FirecrackerInstall FirecrackerInstall = new(
        "v1.7.0",
        "/opt/testdata/firecracker",
        "/opt/testdata/jailer");

    private static string VmId => Random.Shared.NextInt64(100000).ToString();

    public static async Task<Vm> StartUnrestrictedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.JsonConfiguration)
    {
        return await UnrestrictedVm.StartAsync(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            VmId);
    }

    public static async Task<Vm> StartJailedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.JsonConfiguration)
    {
        return await JailedVm.StartAsync(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            JailerOptions,
            VmId);
    }
}