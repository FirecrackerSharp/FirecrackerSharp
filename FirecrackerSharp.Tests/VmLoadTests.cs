using FirecrackerSharp.Core;
using FirecrackerSharp.Tests.Fixtures;
using FirecrackerSharp.Tests.Helpers;
using FluentAssertions;

namespace FirecrackerSharp.Tests;

public class VmLoadTests : MinimalFixture
{
    [LoadTest(10)]
    [InlineData(10, false)]
    [InlineData(3, true)]
    public async Task LowLoad(int load, bool jailed)
    {
        await PerformLoadAsync(load, jailed);
    }
    
    [LoadTest(40)]
    [InlineData(40, false)]
    [InlineData(10, true)]
    public async Task ModerateLoad(int load, bool jailed)
    {
        await PerformLoadAsync(load, jailed);
    }

    [LoadTest(100)]
    [InlineData(100, false)]
    [InlineData(25, true)]
    public async Task HighLoad(int load, bool jailed)
    {
        await PerformLoadAsync(load, jailed);
    }

    [LoadTest(250)]
    [InlineData(250, false)]
    [InlineData(75, true)]
    public async Task ExtremeLoad(int load, bool jailed)
    {
        await PerformLoadAsync(load, jailed);
    }

    private static async Task PerformLoadAsync(int load, bool jailed)
    {
        var bootTasks = new List<Task>();
        var vms = new List<Vm>();

        for (var i = 0; i < load; ++i)
        {
            bootTasks.Add(jailed ? JailedLaunchAsync() : UnrestrictedLaunchAsync());
        }

        await Task.WhenAll(bootTasks);
        
        await Task.Delay(TimeSpan.FromSeconds(15)); // under very high load, VMs initialize slowly
        
        var allGracefulShutdowns = true;
        var shutdownTasks = vms.Select(ShutdownAsync).ToList();

        await Task.WhenAll(shutdownTasks);

        allGracefulShutdowns.Should().BeTrue();

        return;

        async Task UnrestrictedLaunchAsync()
        {
            vms.Add(await VmArrange.StartUnrestrictedVm());
        }
        
        async Task JailedLaunchAsync()
        {
            vms.Add(await VmArrange.StartJailedVm());
        }

        async Task ShutdownAsync(Vm vm)
        {
            var shutdownResult = await vm.ShutdownAsync();
            if (shutdownResult.IsFailure())
            {
                allGracefulShutdowns = false;
                // don't leave healthy VMs running if only one broke, that'd exhaust the runner's system resources
            }
        }
    }
}