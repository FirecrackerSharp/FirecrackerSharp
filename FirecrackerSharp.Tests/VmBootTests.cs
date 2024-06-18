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

        var mob = new MemoryOutputBuffer();
        var ob = new AggregateOutputBuffer([new FileOutputBuffer("/tmp/log.txt"), mob]);
        vm.TtyClient.OutputBuffer = ob;
        var ct = new DelayCompletionTracker(TimeSpan.FromSeconds(2));

        await vm.TtyClient.WriteAsync("df -h", completionTracker: ct);
        await vm.TtyClient.WaitForAvailabilityAsync();
        var commit = mob.LastCommit;

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