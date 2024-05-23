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

        var command = await vm.Terminal.StartCommandAsync("du");
        await command.AwaitAndReadAsync(timeoutSeconds: 100);
        Console.WriteLine(command.CurrentOutput);
    }

    public async Task ShutdownVms()
    {
        await Task.WhenAll(_vms.Select(x => x.ShutdownAsync()));
    }
}