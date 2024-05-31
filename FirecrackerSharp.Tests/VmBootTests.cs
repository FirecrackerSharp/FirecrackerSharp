using FirecrackerSharp.Data;
using FirecrackerSharp.Tests.Helpers;
using FluentAssertions;

namespace FirecrackerSharp.Tests;

public class VmBootTests : MinimalFixture
{
    [Theory]
    [InlineData(VmConfigurationApplicationMode.ThroughJsonConfiguration)]
    [InlineData(VmConfigurationApplicationMode.ThroughApiCalls)]
    public async Task UnrestrictedVm_ShouldBootAndExitGracefully(VmConfigurationApplicationMode configurationApplicationMode)
    {
        var vm = await VmArrange.StartUnrestrictedVm(configurationApplicationMode);

        var gracefulShutdown = await vm.ShutdownAsync();
        gracefulShutdown.Should().BeTrue();
    }

    [Theory]
    [InlineData(VmConfigurationApplicationMode.ThroughJsonConfiguration)]
    [InlineData(VmConfigurationApplicationMode.ThroughApiCalls)]
    public async Task JailedVm_ShouldBootAndExitGracefully(VmConfigurationApplicationMode configurationApplicationMode)
    {
        var vm = await VmArrange.StartJailedVm(configurationApplicationMode);
        
        var gracefulShutdown = await vm.ShutdownAsync();
        gracefulShutdown.Should().BeTrue();
    }
}