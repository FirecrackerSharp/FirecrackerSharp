using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Demo;

public class StressTester(FirecrackerInstall firecrackerInstall, VmConfiguration testConfig)
{
    private List<FirecrackerVm> _vms = [];
    
    public async Task StartVm()
    {
        var vm = await UnrestrictedFirecrackerVm.StartAsync(
            testConfig, firecrackerInstall,
            new FirecrackerOptions(Guid.NewGuid().ToString()),
            vmId: Random.Shared.NextInt64(100000).ToString());
        _vms.Add(vm);
    }

    public async Task ShutdownVms()
    {
        await Task.WhenAll(_vms.Select(x => x.ShutdownAsync()));
    }
}