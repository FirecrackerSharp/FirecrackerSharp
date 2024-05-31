using FirecrackerSharp.Boot;
using FirecrackerSharp.Host.Local;

namespace FirecrackerSharp.Tests.Helpers;

public class SingleVmTestFactory : IAsyncLifetime
{
    public Vm Vm { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        LocalHost.Configure();
        Vm = await VmArrangeUtility.StartUnrestrictedVm();
    }

    public async Task DisposeAsync()
    {
        await Vm.ShutdownAsync();
    }
}