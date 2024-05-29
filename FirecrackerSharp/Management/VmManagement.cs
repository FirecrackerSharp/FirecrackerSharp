using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Actions;
using FirecrackerSharp.Data.Ballooning;

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
    /// Endpoint: "GET /".
    /// </summary>
    /// <returns>The <see cref="ManagementResponse"/> returned from this endpoint with potential content of <see cref="VmInfo"/></returns>
    public async Task<ManagementResponse> GetInfoAsync()
    {
        return await _vm.Socket.GetAsync<VmInfo>("/");
    }

    /// <summary>
    /// Gets the entirety of this microVM's <see cref="VmConfiguration"/> that takes all post-boot modifications into account.
    /// Endpoint: "GET /vm/config".
    /// </summary>
    /// <returns>The <see cref="ManagementResponse"/> returned from this endpoint with potential content of <see cref="VmConfiguration"/></returns>
    public async Task<ManagementResponse> GetCurrentConfigurationAsync()
    {
        return await _vm.Socket.GetAsync<VmConfiguration>("/vm/config");
    }

    /// <summary>
    /// Sends a <see cref="VmAction"/> to this microVM.
    /// Endpoint: "PUT /actions".
    /// </summary>
    /// <param name="action">The <see cref="VmAction"/> to be performed</param>
    /// <returns>The <see cref="ManagementResponse"/> returned that contains no content if successful</returns>
    public async Task<ManagementResponse> PerformActionAsync(VmAction action)
    {
        return await _vm.Socket.PutAsync("/actions", action);
    }

    /// <summary>
    /// Gets the current state of the configured <see cref="VmBalloon"/>, or returns a bad request response if no
    /// balloon was configured pre-boot of the microVM.
    /// Endpoint: "GET /balloon".
    /// </summary>
    /// <returns>The <see cref="ManagementResponse"/> returned containing a <see cref="VmBalloon"/> upon success</returns>
    public async Task<ManagementResponse> GetBalloonAsync()
    {
        return await _vm.Socket.GetAsync<VmBalloon>("/balloon");
    }

    /// <summary>
    /// Updates the configured <see cref="VmBalloon"/> with a <see cref="VmBalloonUpdate"/>.
    /// Endpoint: "PATCH /balloon".
    /// </summary>
    /// <param name="balloonUpdate">The <see cref="VmBalloonUpdate"/> to be performed.</param>
    /// <returns>A no-content <see cref="ManagementResponse"/> if successful</returns>
    public async Task<ManagementResponse> UpdateBalloonAsync(VmBalloonUpdate balloonUpdate)
    {
        return await _vm.Socket.PatchAsync("/balloon", balloonUpdate);
    }

    /// <summary>
    /// Gets the <see cref="VmBalloonStatistics"/> for the configured <see cref="VmBalloon"/> if these statistics were
    /// enabled (<see cref="VmBalloon.StatsPollingIntervalS"/> is greater than 0).
    /// Endpoint: "GET /balloon/statistics".
    /// </summary>
    /// <returns>A <see cref="ManagementResponse"/> with <see cref="VmBalloonStatistics"/> if successful</returns>
    public async Task<ManagementResponse> GetBalloonStatisticsAsync()
    {
        return await _vm.Socket.GetAsync<VmBalloonStatistics>("/balloon/statistics");
    }

    /// <summary>
    /// Updates the <see cref="VmBalloonStatistics"/> with a given <see cref="VmBalloonStatisticsUpdate"/> if the
    /// statistics were enabled in the first place.
    /// Endpoint: "PATCH /balloon/statistics".
    /// </summary>
    /// <param name="balloonStatisticsUpdate">The specified <see cref="VmBalloonStatisticsUpdate"/> to be applied</param>
    /// <returns>A <see cref="ManagementResponse"/> with no content if successful</returns>
    public async Task<ManagementResponse> UpdateBalloonStatisticsAsync(
        VmBalloonStatisticsUpdate balloonStatisticsUpdate)
    {
        return await _vm.Socket.PatchAsync("/balloon/statistics", balloonStatisticsUpdate);
    }
}