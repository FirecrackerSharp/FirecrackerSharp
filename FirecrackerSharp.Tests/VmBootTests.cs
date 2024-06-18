using FirecrackerSharp.Data;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tests.Fixtures;
using FirecrackerSharp.Tests.Helpers;
using FirecrackerSharp.Tty;
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

        var ob = new InMemoryOutputBuffer();
        vm.TtyClient.OutputBuffer = ob;
        var ct = new ExitSignalCompletionTracker(prefix: "ES");
        await vm.TtyClient.WriteAsync("cat /tmp/tmpttt.txt", completionTracker: ct);
        await vm.TtyClient.WaitForAvailabilityAsync();
        var output = ob.Content;
        
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