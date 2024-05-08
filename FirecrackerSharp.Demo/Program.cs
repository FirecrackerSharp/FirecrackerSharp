using FirecrackerSharp.Core;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .CreateLogger();

var testConfig = new VmConfiguration(
    BootSource: new VmBootSource("/home/kanpov/.tmp/vmlinux-5.10.210", "console=ttyS0 reboot=k panic=1 pci=off"),
    MachineConfiguration: new VmMachineConfiguration(128, 1),
    Drives: [new VmDrive("rootfs", true, PathOnHost: "/home/kanpov/.tmp/ubuntu-22.04.ext4")]);


var im = new FirecrackerInstallManager("/home/kanpov/Documents/firecracker");
var install = await im.GetFromIndexAsync("v1.7.0");
var vm = await JailedFirecrackerVm.StartAsync(
    testConfig, install!, new FirecrackerOptions("test"), new JailerOptions("1234", 1000, 1000));
await vm.ShutdownAsync();
