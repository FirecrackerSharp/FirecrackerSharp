using FirecrackerSharp.Boot;
using FirecrackerSharp.Host.Local;
using FirecrackerSharp.Tests.Helpers;

namespace FirecrackerSharp.Tests.Fixtures;

public class SingleVmFixture : IAsyncLifetime
{
    protected Vm Vm { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        LocalHost.Configure();
        Vm = await VmArrange.StartUnrestrictedVm();
    }

    public async Task DisposeAsync()
    {
        await Vm.ShutdownAsync();
    }
}