﻿using FirecrackerSharp.Data;
using FirecrackerSharp.Demo;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Transport;
using FirecrackerSharp.Transport.SSH;
using Renci.SshNet;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .CreateLogger();

IFirecrackerTransport.Current =
    new SshFirecrackerTransport(new ConnectionInfo("192.168.88.112", "root", new PasswordAuthenticationMethod("root", "495762")));

// var im = new FirecrackerInstallManager("/tmp/firecracker");
// var inst = await im.InstallAsync();
// await im.AddToIndexAsync(inst);
// Console.WriteLine(inst);

var testConfig = new VmConfiguration(
    BootSource: new VmBootSource("/home/kanpov/.tmp/vmlinux-5.10.210", "console=ttyS0 reboot=k panic=1 pci=off"),
    MachineConfiguration: new VmMachineConfiguration(128, 1),
    Drives: [new VmDrive("rootfs", true, PathOnHost: "/home/kanpov/.tmp/ubuntu-22.04.ext4")]);
var im = new FirecrackerInstallManager("/home/kanpov/Documents/firecracker");
var install = await im.GetFromIndexAsync("v1.7.0");
var str = new StressTester(install!, testConfig);

var tasks = new List<Task>();
for (var i = 0; i < 100; ++i)
{
    tasks.Add(str.StartVm());
}

await Task.WhenAll(tasks);

Log.Information("ALL VMS BOOTED");
await Task.Delay(3000);
await str.ShutdownVms();
Log.Information("ALL VMS SHUT DOWN");

//
//
// var im = new FirecrackerInstallManager("/home/kanpov/Documents/firecracker");
// var install = await im.GetFromIndexAsync("v1.7.0");
// var vm = await JailedFirecrackerVm.StartAsync(
//     testConfig, install!,
//     new FirecrackerOptions("test"),
//     new JailerOptions(1000, 1000, "495762"),
//     vmId: Random.Shared.NextInt64(100000).ToString());
//
// var str = await vm.SocketHttpClient.GetStringAsync("/");
// Console.WriteLine(str);
// await vm.ShutdownAsync();
