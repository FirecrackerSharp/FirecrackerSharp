using FirecrackerSharp.Data;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tests.Fixtures;
using FirecrackerSharp.Tests.Helpers;
using FirecrackerSharp.Tty;
using FirecrackerSharp.Tty.CompletionTracking;
using FirecrackerSharp.Tty.OutputBuffering;
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

        await vm.TtyClient.StartBufferedCommandAsync("read n && echo $n");
        await vm.TtyClient.WriteIntermittentAsync("test");
        var res = await vm.TtyClient.WaitForBufferedCommandAsync();
        res.Should().NotBeNull();

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