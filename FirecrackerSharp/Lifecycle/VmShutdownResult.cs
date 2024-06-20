namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// Represents the success, failure (could not properly shut down microVM), or soft failure (did not fully shut down
/// microVM) of a microVM shutdown attempt.
/// </summary>
public enum VmShutdownResult
{
    /// <summary>
    /// The shutdown was completely successful, and the post-shutdown cleanup was successful as well.
    /// </summary>
    Successful,
    /// <summary>
    /// The shutdown was successful, however the post-shutdown cleanup failed.
    /// </summary>
    SoftFailedDuringCleanup,
    /// <summary>
    /// The shutdown failed due to a broken I/O pipe, either due to a remote connection issue or (most likely) the
    /// firecracker/jailer process having crashed.
    /// </summary>
    FailedDueToBrokenPipe,
    /// <summary>
    /// The shutdown failed due to the firecracker/jailer process not responding to the dispatched "reboot" TTY command
    /// and hanging instead.
    /// </summary>
    FailedDueToHangingProcess,
    /// <summary>
    /// The shutdown failed due to the primary write of the dispatched "reboot" TTY command not going through the TTY.
    /// </summary>
    FailedDueToTtyNotResponding,
    /// <summary>
    /// The shutdown failed due to an exception that is unknown
    /// </summary>
    FailedDueToUnknownReason
}