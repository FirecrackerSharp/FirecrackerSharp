using FirecrackerSharp.Core;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tests.Helpers;
using FluentAssertions;

namespace FirecrackerSharp.Tests.Vmm;

public class VmLifecycleTests : MinimalFixture
{
    [Fact]
    public async Task ShouldChangePhases()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        vm.Lifecycle.CurrentPhase.Should().Be(VmLifecyclePhase.NotBooted);
        await Task.WhenAll(vm.BootAsync(), CheckIntermittentPhase(VmLifecyclePhase.Booting));
        vm.Lifecycle.CurrentPhase.Should().Be(VmLifecyclePhase.Active);
        await Task.WhenAll(vm.ShutdownAsync(), CheckIntermittentPhase(VmLifecyclePhase.ShuttingDown));
        vm.Lifecycle.CurrentPhase.Should().Be(VmLifecyclePhase.PoweredOff);

        return;

        async Task CheckIntermittentPhase(VmLifecyclePhase phase)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(30));
            vm.Lifecycle.CurrentPhase.Should().Be(phase);
        }
    }

    [Fact]
    public async Task AccessToFaculties_ShouldBeDeniedWhenNotActive()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        AssertAccessForbidden(vm);
        await vm.BootAsync();
        AssertAccessAllowed(vm);
        await vm.ShutdownAsync();
        AssertAccessForbidden(vm);
    }

    [Fact]
    public async Task AccessToBootAndShutdown_ShouldBeAllowedOnlyInCorrectPhases()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        // before boot
        await FluentActions
            .Awaiting(() => vm.ShutdownAsync())
            .Should().ThrowAsync<NotAccessibleDueToLifecycleException>();
        await FluentActions
            .Awaiting(() => vm.BootAsync())
            .Should().NotThrowAsync<NotAccessibleDueToLifecycleException>();
        // after boot
        await FluentActions
            .Awaiting(() => vm.BootAsync())
            .Should().ThrowAsync<NotAccessibleDueToLifecycleException>();
        await FluentActions
            .Awaiting(() => vm.ShutdownAsync())
            .Should().NotThrowAsync<NotAccessibleDueToLifecycleException>();
    }

    private static void AssertAccessForbidden(Vm vm)
    {
        FluentActions.Invoking(() => vm.Management).Should().Throw<NotAccessibleDueToLifecycleException>();
        FluentActions.Invoking(() => vm.TtyClient).Should().Throw<NotAccessibleDueToLifecycleException>();
        FluentActions.Invoking(() => vm.Lifecycle).Should().NotThrow<NotAccessibleDueToLifecycleException>();
    }

    private static void AssertAccessAllowed(Vm vm)
    {
        FluentActions.Invoking(() => vm.Management).Should().NotThrow<NotAccessibleDueToLifecycleException>();
        FluentActions.Invoking(() => vm.TtyClient).Should().NotThrow<NotAccessibleDueToLifecycleException>();
        FluentActions.Invoking(() => vm.Lifecycle).Should().NotThrow<NotAccessibleDueToLifecycleException>();
    }
}