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
public sealed class VmManagement
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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>The <see cref="ResponseWith{VmInfo}"/> response</returns>
    public async Task<ResponseWith<VmInfo>> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.GetAsync<VmInfo>("/", cancellationToken);
    }

    /// <summary>
    /// Gets the entirety of this microVM's <see cref="VmConfiguration"/> that takes all post-boot modifications into account.
    /// Endpoint: "GET /vm/config".
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>The <see cref="ResponseWith{VmConfiguration}"/> response</returns>
    public async Task<ResponseWith<VmConfiguration>> GetCurrentConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.GetAsync<VmConfiguration>("/vm/config", cancellationToken);
    }

    /// <summary>
    /// Sends a <see cref="VmAction"/> to this microVM.
    /// Endpoint: "PUT /actions".
    /// </summary>
    /// <param name="action">The <see cref="VmAction"/> to be performed</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>The <see cref="Response"/> with no content</returns>
    public async Task<Response> PerformActionAsync(
        VmAction action,
        CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.PutAsync("/actions", action, cancellationToken);
    }

    /// <summary>
    /// Gets the current state of the configured <see cref="VmBalloon"/>, or returns a bad request response if no
    /// balloon was configured pre-boot of the microVM.
    /// Endpoint: "GET /balloon".
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>The <see cref="ResponseWith{VmBalloon}"/> response</returns>
    public async Task<ResponseWith<VmBalloon>> GetBalloonAsync(CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.GetAsync<VmBalloon>("/balloon", cancellationToken);
    }

    /// <summary>
    /// Updates the configured <see cref="VmBalloon"/> with a <see cref="VmBalloonUpdate"/>.
    /// Endpoint: "PATCH /balloon".
    /// </summary>
    /// <param name="balloonUpdate">The <see cref="VmBalloonUpdate"/> to be performed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>A <see cref="Response"/> with no content</returns>
    public async Task<Response> UpdateBalloonAsync(
        VmBalloonUpdate balloonUpdate,
        CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.PatchAsync("/balloon", balloonUpdate, cancellationToken);
    }

    /// <summary>
    /// Gets the <see cref="VmBalloonStatistics"/> for the configured <see cref="VmBalloon"/> if these statistics were
    /// enabled (<see cref="VmBalloon.StatsPollingIntervalS"/> is greater than 0).
    /// Endpoint: "GET /balloon/statistics".
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>A <see cref="ResponseWith{VmBalloonStatistics}"/> response</returns>
    public async Task<ResponseWith<VmBalloonStatistics>> GetBalloonStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.GetAsync<VmBalloonStatistics>("/balloon/statistics", cancellationToken);
    }

    /// <summary>
    /// Updates the <see cref="VmBalloonStatistics"/> with a given <see cref="VmBalloonStatisticsUpdate"/> if the
    /// statistics were enabled in the first place.
    /// Endpoint: "PATCH /balloon/statistics".
    /// </summary>
    /// <param name="balloonStatisticsUpdate">The specified <see cref="VmBalloonStatisticsUpdate"/> to be applied</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>A <see cref="Response"/> with no content</returns>
    public async Task<Response> UpdateBalloonStatisticsAsync(
        VmBalloonStatisticsUpdate balloonStatisticsUpdate,
        CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.PatchAsync("/balloon/statistics", balloonStatisticsUpdate, cancellationToken);
    }

    /// <summary>
    /// Updates a <see cref="VmNetworkInterface"/>'s <see cref="VmRateLimiter"/>s according to the <see cref="VmNetworkInterfaceUpdate"/>
    /// 's data. <see cref="VmNetworkInterfaceUpdate.IfaceId"/> is used to determine which <see cref="VmNetworkInterface"/>
    /// needs to be updated.
    /// Endpoint: "PATCH /network-interfaces/{iface-id}".
    /// </summary>
    /// <param name="networkInterfaceUpdate">The <see cref="VmNetworkInterfaceUpdate"/> to be applied</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>A <see cref="Response"/> with no content</returns>
    public async Task<Response> UpdateNetworkInterfaceAsync(
        VmNetworkInterfaceUpdate networkInterfaceUpdate,
        CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.PatchAsync($"/network-interfaces/{networkInterfaceUpdate.IfaceId}",
            networkInterfaceUpdate, cancellationToken);
    }

    /// <summary>
    /// Updates the microVM's <see cref="VmState"/> to one that's specified in the <see cref="VmStateUpdate"/>.
    /// Endpoint: "PATCH /vm".
    /// </summary>
    /// <param name="stateUpdate">The <see cref="VmStateUpdate"/> to be applied</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>A <see cref="Response"/> with no content</returns>
    public async Task<Response> UpdateStateAsync(
        VmStateUpdate stateUpdate,
        CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.PatchAsync("/vm", stateUpdate, cancellationToken);
    }

    /// <summary>
    /// Updates a <see cref="VmDrive"/> according to the <see cref="VmDriveUpdate"/>'s data.
    /// Endpoint: "PATCH /drives/{drive-id}".
    /// </summary>
    /// <param name="driveUpdate">The <see cref="VmDriveUpdate"/> to be applied</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>The <see cref="Response"/> with no content</returns>
    public async Task<Response> UpdateDriveAsync(
        VmDriveUpdate driveUpdate,
        CancellationToken cancellationToken = default)
    {
        return await _vm.Socket.PatchAsync($"/drives/{driveUpdate.DriveId}", driveUpdate, cancellationToken);
    }

    internal async Task<bool> ApplyVmConfigurationAsync(bool parallelize, CancellationToken cancellationToken)
    {
        var allResponsesAreSuccessful = true;
        
        var tasks = new List<Task>
        {
            Wrap(() =>_vm.Socket.PutAsync("/boot-source", _vm.VmConfiguration.BootSource, cancellationToken)),
            Wrap(() => _vm.Socket.PutAsync("/machine-config", _vm.VmConfiguration.MachineConfiguration, cancellationToken))
        };

        tasks.AddRange(_vm.VmConfiguration.Drives
            .Select(drive => Wrap(() => _vm.Socket.PutAsync($"/drives/{drive.DriveId}", drive, cancellationToken))));

        if (_vm.VmConfiguration.NetworkInterfaces is not null)
        {
            tasks.AddRange(_vm.VmConfiguration.NetworkInterfaces
                .Select(networkInterface =>
                    Wrap(() => _vm.Socket.PutAsync($"/network-interfaces/{networkInterface.IfaceId}", networkInterface, cancellationToken))));
        }

        if (_vm.VmConfiguration.Balloon is not null)
        {
            tasks.Add(Wrap(() =>_vm.Socket.PutAsync("/balloon", _vm.VmConfiguration.Balloon, cancellationToken)));
        }

        if (_vm.VmConfiguration.Logger is not null)
        {
            tasks.Add(Wrap(() => _vm.Socket.PutAsync("/logger", _vm.VmConfiguration.Logger, cancellationToken)));
        }

        if (_vm.VmConfiguration.Metrics is not null)
        {
            tasks.Add(Wrap(() => _vm.Socket.PutAsync("/metrics", _vm.VmConfiguration.Metrics, cancellationToken)));
        }

        if (_vm.VmConfiguration.EntropyDevice is not null)
        {
            tasks.Add(Wrap(() =>_vm.Socket.PutAsync("/entropy", _vm.VmConfiguration.EntropyDevice, cancellationToken)));
        }

        if (_vm.VmConfiguration.Vsock is not null)
        {
            tasks.Add(Wrap(() => _vm.Socket.PutAsync("/vsock", _vm.VmConfiguration.Vsock, cancellationToken)));
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

        return allResponsesAreSuccessful;

        async Task Wrap(Func<Task<Response>> action)
        {
            var response = await action();
            if (response != Response.Success) allResponsesAreSuccessful = false;
        }
    }

    /// <summary>
    /// Static messages that are meant to be reused by multiple VM host implementations as errors inside faulty
    /// Management API responses.
    /// </summary>
    public static class Problems
    {
        public const string TimedOut = "request failed: timed out according to the given cancellation token";
        public const string CouldNotConnect = "request failed: could not connect";
        public const string CouldNotReceiveStatusCode = "request failed: could not read HTTP status code";
        public const string CouldNotReadResponseBody = "request failed: could not read response body";
        public const string CouldNotParseFaultMessage = "could not parse fault message";
        public const string CouldNotDeserializeJson = "request failed: could not deserialize JSON";
    }
}