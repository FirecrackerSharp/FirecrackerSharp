using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;

namespace FirecrackerSharp.Management;

public class VmManagement
{
    private readonly Vm _vm;
    
    internal VmManagement(Vm vm)
    {
        _vm = vm;
    }

    public async Task<ManagementResponse> GetInfoAsync()
    {
        return await _vm.Socket.GetAsync<VmInfo>("/");
    }

    public async Task<ManagementResponse> GetCurrentConfigurationAsync()
    {
        return await _vm.Socket.GetAsync<VmConfiguration>("/vm/config");
    }
}