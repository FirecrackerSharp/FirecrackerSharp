using FirecrackerSharp.Data;
using FluentAssertions;

namespace FirecrackerSharp.Tests;

public class VmBootTests : GenericFixture
{
    [Theory]
    [InlineData(VmConfigurationApplicationMode.ThroughJsonConfiguration)]
    [InlineData(VmConfigurationApplicationMode.ThroughApiCalls)]
    public async Task UnrestrictedVm_ShouldBootAndExitGracefully(VmConfigurationApplicationMode configurationApplicationMode)
    {
        var vm = await VmArrangeUtility.StartUnrestrictedVm(configurationApplicationMode);

        var gracefulShutdown = await vm.ShutdownAsync();
        gracefulShutdown.Should().BeTrue();
    }

    [Theory]
    [InlineData(VmConfigurationApplicationMode.ThroughJsonConfiguration)]
    [InlineData(VmConfigurationApplicationMode.ThroughApiCalls)]
    public async Task JailedVm_ShouldBootAndExitGracefully(VmConfigurationApplicationMode configurationApplicationMode)
    {
        var vm = await VmArrangeUtility.StartJailedVm(configurationApplicationMode);
        
        var gracefulShutdown = await vm.ShutdownAsync();
        gracefulShutdown.Should().BeTrue();
    }
}