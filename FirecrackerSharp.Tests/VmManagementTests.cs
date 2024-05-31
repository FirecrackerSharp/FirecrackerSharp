using FirecrackerSharp.Data;
using FirecrackerSharp.Data.State;
using FirecrackerSharp.Tests.Helpers;

namespace FirecrackerSharp.Tests;

public class VmManagementTests(SingleVmTestFactory testFactory) : SingleVmTest(testFactory)
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
}