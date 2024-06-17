using FirecrackerSharp.Core;
using FirecrackerSharp.Data;
using FirecrackerSharp.Data.Actions;
using FirecrackerSharp.Data.Ballooning;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Data.State;

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

    /// <summary>
    /// Updates a <see cref="VmNetworkInterface"/>'s <see cref="VmRateLimiter"/>s according to the <see cref="VmNetworkInterfaceUpdate"/>
    /// 's data. <see cref="VmNetworkInterfaceUpdate.IfaceId"/> is used to determine which <see cref="VmNetworkInterface"/>
    /// needs to be updated.
    /// Endpoint: "PATCH /network-interfaces/{iface-id}".
    /// </summary>
    /// <param name="networkInterfaceUpdate">The <see cref="VmNetworkInterfaceUpdate"/> to be applied</param>
    /// <returns>A <see cref="ManagementResponse"/> with no content if successful</returns>
    public async Task<ManagementResponse> UpdateNetworkInterfaceAsync(VmNetworkInterfaceUpdate networkInterfaceUpdate)
    {
        return await _vm.Socket.PatchAsync($"/network-interfaces/{networkInterfaceUpdate.IfaceId}",
            networkInterfaceUpdate);
    }

    /// <summary>
    /// Updates the microVM's <see cref="VmState"/> to one that's specified in the <see cref="VmStateUpdate"/>.
    /// Endpoint: "PATCH /vm".
    /// </summary>
    /// <param name="stateUpdate">The <see cref="VmStateUpdate"/> to be applied</param>
    /// <returns>A <see cref="ManagementResponse"/> with no content if successful</returns>
    public async Task<ManagementResponse> UpdateStateAsync(VmStateUpdate stateUpdate)
    {
        return await _vm.Socket.PatchAsync("/vm", stateUpdate);
    }

    /// <summary>
    /// Updates a <see cref="VmDrive"/> according to the <see cref="VmDriveUpdate"/>'s data.
    /// Endpoint: "PATCH /drives/{drive-id}".
    /// </summary>
    /// <param name="driveUpdate">The <see cref="VmDriveUpdate"/> to be applied</param>
    /// <returns>The <see cref="ManagementResponse"/> containing no content if successful</returns>
    public async Task<ManagementResponse> UpdateDriveAsync(VmDriveUpdate driveUpdate)
    {
        return await _vm.Socket.PatchAsync($"/drives/{driveUpdate.DriveId}", driveUpdate);
    }

    internal async Task ApplyVmConfigurationAsync(bool parallelize)
    {
        var tasks = new List<Task>
        {
            _vm.Socket.PutAsync("/boot-source", _vm.VmConfiguration.BootSource),
            _vm.Socket.PutAsync("/machine-config", _vm.VmConfiguration.MachineConfiguration)
        };

        tasks.AddRange(_vm.VmConfiguration.Drives
            .Select(drive => _vm.Socket.PutAsync($"/drives/{drive.DriveId}", drive)));

        if (_vm.VmConfiguration.NetworkInterfaces is not null)
        {
            tasks.AddRange(_vm.VmConfiguration.NetworkInterfaces
                .Select(networkInterface => _vm.Socket.PutAsync($"/network-interfaces/{networkInterface.IfaceId}", networkInterface)));
        }

        if (_vm.VmConfiguration.Balloon is not null)
        {
            tasks.Add(_vm.Socket.PutAsync("/balloon", _vm.VmConfiguration.Balloon));
        }

        if (_vm.VmConfiguration.Logger is not null)
        {
            tasks.Add(_vm.Socket.PutAsync("/logger", _vm.VmConfiguration.Logger));
        }

        if (_vm.VmConfiguration.Metrics is not null)
        {
            tasks.Add(_vm.Socket.PutAsync("/metrics", _vm.VmConfiguration.Metrics));
        }

        if (_vm.VmConfiguration.EntropyDevice is not null)
        {
            tasks.Add(_vm.Socket.PutAsync("/entropy", _vm.VmConfiguration.EntropyDevice));
        }

        if (_vm.VmConfiguration.Vsock is not null)
        {
            tasks.Add(_vm.Socket.PutAsync("/vsock", _vm.VmConfiguration.Vsock));
        }

        if (parallelize)
        {
            await Task.WhenAll(tasks);
        }
        else
        {
            foreach (var task in tasks)
            {
                await task;
            }
        }
    }
}