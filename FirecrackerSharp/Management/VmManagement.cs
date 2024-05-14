using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;

namespace FirecrackerSharp.Management;

/// <summary>
/// An interface to the Firecracker Management API of a certain microVM.
/// 
/// This API allows various pre-boot and post-boot operations to be performed with the microVM, however any pre-boot
/// API functionality is intentionally unsupported by this SDK as we believe that encapsulating pre-boot microVM
/// configuration through <see cref="VmConfiguration"/> instead of API calls makes more practical sense.
/// </summary>
public class VmManagement
{
    private readonly Vm _vm;
    
    internal VmManagement(Vm vm)
    {
        _vm = vm;
    }

    /// <summary>
    /// Gets the information (state) of this microVM (<see cref="VmInfo"/>).
    ///
    /// This corresponds to the "GET /" endpoint of the underlying API.
    /// </summary>
    /// <returns>The <see cref="ManagementResponse"/> returned from this endpoint with potential content of <see cref="VmInfo"/></returns>
    public async Task<ManagementResponse> GetInfoAsync()
    {
        return await _vm.Socket.GetAsync<VmInfo>("/");
    }

    /// <summary>
    /// Gets the entirety of this microVM's <see cref="VmConfiguration"/> that takes all post-boot modifications into account.
    ///
    /// This corresponds to the "GET /vm/config" endpoint of the underlying API.
    /// </summary>
    /// <returns></returns>
    public async Task<ManagementResponse> GetCurrentConfigurationAsync()
    {
        return await _vm.Socket.GetAsync<VmConfiguration>("/vm/config");
    }
}