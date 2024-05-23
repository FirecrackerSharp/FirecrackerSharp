using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Tty;

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

        for (var i = 0; i < 10; ++i)
        {
            var command = await vm.Tty.StartCommandAsync(
                new TtyCommandOptions("top", [], TimeSpan.FromMilliseconds(500), "q", false));
            await command.AwaitAndReadAsync(timeoutSeconds: 1);
            Console.Write(command.CurrentOutput);
            await command.StopAsync();
            Console.Write(command.CurrentOutput);
        }
    }

    public async Task ShutdownVms()
    {
        await Task.WhenAll(_vms.Select(x => x.ShutdownAsync()));
    }
}