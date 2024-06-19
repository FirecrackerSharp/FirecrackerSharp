using System.Text;
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

    [Fact]
    public async Task AttachAllLogTargets_ShouldRedirect()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        var bootSb = new StringBuilder();
        var activeSb = new StringBuilder();
        var shutdownSb = new StringBuilder();
        vm.Lifecycle.AttachAllLogTargets(ILogTarget.ToStringBuilder(bootSb), ILogTarget.ToStringBuilder(activeSb), ILogTarget.ToStringBuilder(shutdownSb));
        
        await vm.BootAsync();
        await vm.TtyClient.RunBufferedCommandAsync("cat --help");
        await vm.ShutdownAsync();
        
        bootSb.ToString().Should().Contain("Artificially kick devices");
        activeSb.ToString().Should().Contain("cat --help");
        shutdownSb.ToString().Should().Contain("Firecracker exiting successfully");
    }

    [Fact]
    public async Task DetachAllLogTargets_ShouldNotRedirect()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        var bootSb = new StringBuilder();
        var activeSb = new StringBuilder();
        var shutdownSb = new StringBuilder();
        vm.Lifecycle.AttachAllLogTargets(ILogTarget.ToStringBuilder(bootSb), ILogTarget.ToStringBuilder(activeSb), ILogTarget.ToStringBuilder(shutdownSb));
        vm.Lifecycle.DetachAllLogTargets();

        await vm.BootAsync();
        await vm.TtyClient.RunBufferedCommandAsync("cat --help");
        await vm.ShutdownAsync();

        bootSb.ToString().Should().BeEmpty();
        activeSb.ToString().Should().BeEmpty();
        shutdownSb.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task DetachLogTarget_ShouldNotRedirect()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        var sb = new StringBuilder();
        vm.Lifecycle.AttachLogTarget(LoggedVmLifecyclePhase.Active, ILogTarget.ToStringBuilder(sb));
        vm.Lifecycle.DetachLogTarget(LoggedVmLifecyclePhase.Active);

        await vm.BootAsync();
        await vm.TtyClient.RunBufferedCommandAsync("cat --help");
        await vm.ShutdownAsync();

        sb.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task AttachLogTarget_ShouldRedirect()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        var sb = new StringBuilder();
        vm.Lifecycle.AttachLogTarget(LoggedVmLifecyclePhase.Boot, ILogTarget.ToStringBuilder(sb));

        await vm.BootAsync();
        await vm.ShutdownAsync();

        sb.ToString().Should().Contain("Artificially kick devices");
    }

    [Fact]
    public async Task AttachAllLogTargetsToSingle_ShouldRedirectToSingle()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        var sb = new StringBuilder();
        vm.Lifecycle.AttachAllLogTargetsToSingle(ILogTarget.ToStringBuilder(sb));

        await vm.BootAsync();
        await vm.TtyClient.RunBufferedCommandAsync("cat --help");
        await vm.ShutdownAsync();

        sb.ToString().Should().Contain("Artificially kick devices")
            .And.Contain("cat --help")
            .And.Contain("Firecracker exiting successfully");
    }

    [Fact]
    public void IsLogTargetAttached_ShouldCorrespondToAttachment()
    {
        var vm = VmArrange.GetUnrestrictedVm();
        var sb = new StringBuilder();
        vm.Lifecycle.AttachLogTarget(LoggedVmLifecyclePhase.Active, ILogTarget.ToStringBuilder(sb));
        vm.Lifecycle.IsLogTargetAttached(LoggedVmLifecyclePhase.Active).Should().BeTrue();
        vm.Lifecycle.DetachLogTarget(LoggedVmLifecyclePhase.Active);
        vm.Lifecycle.IsLogTargetAttached(LoggedVmLifecyclePhase.Active).Should().BeFalse();
    }

    [Fact]
    public async Task PhaseChangeEvents_ShouldFireInCorrectOrder()
    {
        var eventOrder = new List<(VmLifecyclePhase, bool)>(); // phase -> started (true), finished (false)
        var vm = VmArrange.GetUnrestrictedVm();
        vm.Lifecycle.PhaseStarted += (_, phase) =>
        {
            eventOrder.Add((phase, true));
        };
        vm.Lifecycle.PhaseFinished += (_, phase) =>
        {
            eventOrder.Add((phase, false));
        };

        await vm.BootAsync();
        await vm.ShutdownAsync();

        eventOrder.Should().BeEquivalentTo([
            (VmLifecyclePhase.NotBooted, false), // finished being not booted
            (VmLifecyclePhase.Booting, true), // started booting
            (VmLifecyclePhase.Booting, false), // finished booting
            (VmLifecyclePhase.Active, true), // started being active
            (VmLifecyclePhase.Active, false), // finished being active
            (VmLifecyclePhase.ShuttingDown, true), // started shutting down
            (VmLifecyclePhase.ShuttingDown, false), // finished shutting down
            (VmLifecyclePhase.PoweredOff, true) // started being powered off
        ]);
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