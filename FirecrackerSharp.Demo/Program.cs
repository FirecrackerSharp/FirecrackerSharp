using FirecrackerSharp.Core;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

var testConfig = new VmConfiguration(
    BootSource: new VmBootSource("/home/kanpov/.tmp/vmlinux-5.10.210", "console=ttyS0 reboot=k panic=1 pci=off"),
    MachineConfiguration: new VmMachineConfiguration(128, 1),
    Drives: [new VmDrive("rootfs", true, PathOnHost: "/home/kanpov/.tmp/ubuntu-22.04.ext4")]);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var im = new FirecrackerInstallManager("/home/kanpov/Documents/firecracker");
var install = await im.GetFromIndexAsync("v1.7.0");
var vm = await FirecrackerVm.StartAsync(testConfig, install!);
await Task.Delay(2000);
await vm.DisposeAsync();
