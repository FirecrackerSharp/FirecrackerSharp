using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Actions;
using FirecrackerSharp.Data.Ballooning;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Data.State;
using FirecrackerSharp.Tests.Helpers;
using FluentAssertions;

namespace FirecrackerSharp.Tests.Vmm;

public class VmManagementTests : SingleVmFixture
{
    [Fact]
    public async Task GetInfoAsync_ShouldSucceed()
    {
        var response = await Vm.Management.GetInfoAsync();
        response.ShouldSucceedWith<VmInfo>();
    }

    [Fact]
    public async Task GetCurrentConfigurationAsync_ShouldSucceed()
    {
        var response = await Vm.Management.GetCurrentConfigurationAsync();
        response.ShouldSucceedWith<VmConfiguration>();
    }

    [Theory]
    [InlineData(VmActionType.FlushMetrics)]
    [InlineData(VmActionType.SendCtrlAltDel)]
    public async Task PerformActionAsync_ShouldSucceed(VmActionType vmActionType)
    {
        var response = await Vm.Management.PerformActionAsync(new VmAction(vmActionType));
        response.ShouldSucceed();
    }

    [Fact]
    public async Task GetBalloonAsync_ShouldSucceed()
    {
        var response = await Vm.Management.GetBalloonAsync();
        response.ShouldSucceedWith<VmBalloon>();
    }

    [Fact]
    public async Task UpdateBalloonAsync_ShouldReflectChanges()
    {
        const int newBalloonAmountMib = 256;
        
        var updateResponse = await Vm.Management.UpdateBalloonAsync(new VmBalloonUpdate(newBalloonAmountMib));
        updateResponse.ShouldSucceed();

        var checkResponse = await Vm.Management.GetBalloonAsync();
        checkResponse.ShouldSucceedWith<VmBalloon>()
            .AmountMib.Should().Be(newBalloonAmountMib);
    }

    [Fact]
    public async Task GetBalloonStatisticsAsync_ShouldSucceed()
    {
        var response = await Vm.Management.GetBalloonStatisticsAsync();
        response.ShouldSucceedWith<VmBalloonStatistics>();
    }

    [Fact]
    public async Task UpdateBalloonStatisticsAsync_ShouldSucceed()
    {
        const int newPollingInterval = 3;

        var updateResponse =
            await Vm.Management.UpdateBalloonStatisticsAsync(new VmBalloonStatisticsUpdate(newPollingInterval));
        updateResponse.ShouldSucceed();
    }

    [Fact]
    public async Task UpdateStateAsync_CanPauseAndResumeVm()
    {
        var pauseResponse = await Vm.Management.UpdateStateAsync(new VmStateUpdate(VmStateForUpdate.Paused));
        pauseResponse.ShouldSucceed();

        var resumeResponse = await Vm.Management.UpdateStateAsync(new VmStateUpdate(VmStateForUpdate.Resumed));
        resumeResponse.ShouldSucceed();
    }

    [Fact]
    public async Task UpdateDriveAsync_ShouldSucceed()
    {
        var response = await Vm.Management.UpdateDriveAsync(new VmDriveUpdate(
            "rootfs", RateLimiter: new VmRateLimiter(
                new VmTokenBucket(10, 10, 10),
                new VmTokenBucket(10, 10, 10))));
        response.ShouldSucceed();
    }
}