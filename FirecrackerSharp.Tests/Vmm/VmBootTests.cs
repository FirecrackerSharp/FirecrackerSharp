using FirecrackerSharp.Data;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tests.Helpers;
using FirecrackerSharp.Tty.CompletionTracking;
using FluentAssertions;

namespace FirecrackerSharp.Tests.Vmm;

public class VmBootTests : MinimalFixture
{
    [Theory]
    [InlineData(VmConfigurationApplicationMode.JsonConfiguration)]
    [InlineData(VmConfigurationApplicationMode.SequentialApiCalls)]
    [InlineData(VmConfigurationApplicationMode.ParallelizedApiCalls)]
    public async Task UnrestrictedVm_ShouldBootAndExitGracefully(VmConfigurationApplicationMode configurationApplicationMode)
    {
        var vm = await VmArrange.StartUnrestrictedVm(configurationApplicationMode);

        await vm.TtyClient.StartBufferedCommandAsync("read n && echo q$n");
        await vm.TtyClient.WriteIntermittentAsync("test", completionTracker:
            new StringMatchCompletionTracker(StringMatchMode.Contains, "qtest"));
        await vm.TtyClient.WaitForIntermittentAvailabilityAsync();
        var buf = await vm.TtyClient.WaitForBufferedCommandAsync();

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