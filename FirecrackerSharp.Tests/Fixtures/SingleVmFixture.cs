using FirecrackerSharp.Core;
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
        Vm.Lifecycle.BootLogTarget = ILogTarget.Null;
    }

    public async Task DisposeAsync()
    {
        await Vm.ShutdownAsync();
    }
}