namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// Represents the failure or success of a microVM boot attempt.
/// </summary>
public enum VmBootResult
{
    /// <summary>
    /// The boot was completely successful.
    /// </summary>
    Successful,
    /// <summary>
    /// The boot failed during the preparation and invocation of the firecracker/jailer binary.
    /// </summary>
    FailedDuringProcessInvocation,
    /// <summary>
    /// The boot failed due to at least one of the boot API requests timing out or returning an unsuccessful response.
    /// </summary>
    FailedDueToApiRequestFailure,
    /// <summary>
    /// The boot failed due to TTY authentication not being written properly.
    /// </summary>
    FailedDuringTtyAuthentication,
    /// <summary>
    /// The boot failed due to an unknown reason.
    /// </summary>
    FailedDueToUnknownReason
}