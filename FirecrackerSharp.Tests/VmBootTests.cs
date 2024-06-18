using System.Text;
using FirecrackerSharp.Data;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tests.Fixtures;
using FirecrackerSharp.Tests.Helpers;
using FluentAssertions;

namespace FirecrackerSharp.Tests;

public class VmBootTests : MinimalFixture
{
    [Theory]
    [InlineData(VmConfigurationApplicationMode.JsonConfiguration)]
    [InlineData(VmConfigurationApplicationMode.SequentialApiCalls)]
    [InlineData(VmConfigurationApplicationMode.ParallelizedApiCalls)]
    public async Task UnrestrictedVm_ShouldBootAndExitGracefully(VmConfigurationApplicationMode configurationApplicationMode)
    {
        var vm = await VmArrange.StartUnrestrictedVm(configurationApplicationMode);

        var sb = new StringBuilder();

        for (var i = 0; i < 5; ++i)
        {
            await vm.TtyClient.StartAndAwaitCommandAsync("df -h", captureBufferLogTarget: ILogTarget.ToStringBuilder(sb));
        }

        var shutdownResult = await vm.ShutdownAsync();
        shutdownResult.IsSuccessful().Should().BeTrue();
    }

    [Theory]
    [InlineData(VmConfigurationApplicationMode.JsonConfiguration)]
    [InlineData(VmConfigurationApplicationMode.SequentialApiCalls)]
    [InlineData(VmConfigurationApplicationMode.ParallelizedApiCalls)]
    public async Task JailedVm_ShouldBootAndExitGracefully(VmConfigurationApplicationMode configurationApplicationMode)
    {
        var vm = await VmArrange.StartJailedVm(configurationApplicationMode);
        
        var shutdownResult = await vm.ShutdownAsync();
        shutdownResult.IsSuccessful().Should().BeTrue();
    }
}