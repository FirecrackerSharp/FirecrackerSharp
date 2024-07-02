using FirecrackerSharp.Core;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Ballooning;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Lifecycle;
using FluentAssertions;

namespace FirecrackerSharp.Tests.Helpers;

public static class VmArrange
{
    private static readonly VmConfiguration VmConfiguration = new(
        new VmBootSource("/opt/testdata/vmlinux-5.10.217", "console=ttyS0 reboot=k panic=1 pci=off"),
        new VmMachineConfiguration(256, 1),
        Drives: [new VmDrive("rootfs", IsRootDevice: true, PathOnHost: "/opt/testdata/ubuntu-22.04.ext4")],
        Balloon: new VmBalloon(AmountMib: 128, DeflateOnOom: false, StatsPollingIntervalS: 1));

    private static readonly FirecrackerOptions FirecrackerOptions = new(Guid.NewGuid().ToString());

    private static readonly JailerOptions JailerOptions = new(1000, 1000, SudoPassword: Environment.GetEnvironmentVariable("FSH_ROOT_PASSWORD"));

    private static readonly FirecrackerInstall FirecrackerInstall = new(
        "v1.7.0", "/opt/testdata/firecracker", "/opt/testdata/jailer");

    private static string VmId => Random.Shared.NextInt64(100000).ToString();

    public static async Task<Vm> StartUnrestrictedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.JsonConfiguration)
    {
        var unrestrictedVm = new UnrestrictedVm(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            VmId);
        unrestrictedVm.Lifecycle.AttachAllLogTargetsToSingle(ILogTarget.ToFile("/tmp/log.txt"));
        var bootResult = await unrestrictedVm.BootAsync();
        bootResult.IsSuccessful().Should().BeTrue();
        return unrestrictedVm;
    }

    public static Vm GetUnrestrictedVm() => new UnrestrictedVm(VmConfiguration, FirecrackerInstall, FirecrackerOptions, VmId);

    public static async Task<Vm> StartJailedVm(VmConfigurationApplicationMode configurationApplicationMode
        = VmConfigurationApplicationMode.JsonConfiguration)
    {
        var jailedVm = new JailedVm(
            VmConfiguration with { ApplicationMode = configurationApplicationMode },
            FirecrackerInstall,
            FirecrackerOptions,
            JailerOptions,
            VmId);
        var bootResult = await jailedVm.BootAsync();
        bootResult.IsSuccessful().Should().BeTrue();
        return jailedVm;
    }
}