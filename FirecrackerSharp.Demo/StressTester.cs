using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Demo;

public class StressTester(FirecrackerInstall firecrackerInstall, VmConfiguration testConfig)
{
    private readonly List<Vm> _vms = [];
    
    public async Task StartVm()
    {
        var vm = await UnrestrictedVm.StartAsync(
            testConfig, firecrackerInstall,
            new FirecrackerOptions(Guid.NewGuid().ToString()),
            //new JailerOptions(1000, 1000, "495762"),
            vmId: Random.Shared.NextInt64(100000).ToString());
        _vms.Add(vm);

        await Task.Delay(3000);
        // await vm.ShellManager.WriteToTtyAsync("root", new CancellationToken());
        // await vm.ShellManager.WriteToTtyAsync("", new CancellationToken());
        
        var shell = await vm.ShellManager.StartShellAsync();
        var command = await shell.StartCommandAsync("ls");
        Console.Write(await vm.ShellManager.ReadFromTtyAsync(false, new CancellationToken()));
    }

    public async Task ShutdownVms()
    {
        await Task.WhenAll(_vms.Select(x => x.ShutdownAsync()));
    }
}