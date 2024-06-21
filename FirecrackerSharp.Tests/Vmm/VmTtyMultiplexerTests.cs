using FirecrackerSharp.Tests.Helpers;

namespace FirecrackerSharp.Tests.Vmm;

public class VmTtyMultiplexerTests : SingleVmFixture
{
    [Fact]
    public async Task LaunchSession()
    {
        var session = await Vm.TtyMultiplexer.StartSessionAsync();
        var command = await session.StartCommandAsync("cat --help");
    }
}