using FluentAssertions;

namespace FirecrackerSharp.Tests;

public class VmBootTests : GenericFixture
{
    [Fact]
    public async Task UnrestrictedVm_ShouldBootAndExitGracefully()
    {
        var vm = await VmArrangeUtility.StartUnrestrictedVm();

        var gracefulShutdown = await vm.ShutdownAsync();
        gracefulShutdown.Should().BeTrue();
    }

    [Fact]
    public async Task JailedVm_ShouldBootAndExitGracefully()
    {
        var vm = await VmArrangeUtility.StartJailedVm();
        
        var gracefulShutdown = await vm.ShutdownAsync();
        gracefulShutdown.Should().BeTrue();
    }
}