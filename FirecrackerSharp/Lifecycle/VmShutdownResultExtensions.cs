namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// Convenience extension functions for <see cref="VmShutdownResult"/>s.
/// </summary>
public static class VmShutdownResultExtensions
{
    /// <summary>
    /// Whether this <see cref="VmShutdownResult"/> is strictly successful.
    /// </summary>
    /// <param name="shutdownResult">The <see cref="VmShutdownResult"/> to check</param>
    /// <returns>The check result</returns>
    public static bool IsSuccessful(this VmShutdownResult shutdownResult)
        => shutdownResult == VmShutdownResult.Successful;

    /// <summary>
    /// Whether this <see cref="VmShutdownResult"/> is strictly a soft failure.
    /// </summary>
    /// <param name="shutdownResult">The <see cref="VmShutdownResult"/> to check</param>
    /// <returns>The check result</returns>
    public static bool IsSoftFailure(this VmShutdownResult shutdownResult)
        => shutdownResult == VmShutdownResult.SoftFailedDuringCleanup;
    
    /// <summary>
    /// Whether this <see cref="VmShutdownResult"/> is strictly a (hard) failure.
    /// </summary>
    /// <param name="shutdownResult">The <see cref="VmShutdownResult"/> to check</param>
    /// <returns>The check result</returns>
    public static bool IsFailure(this VmShutdownResult shutdownResult)
        => shutdownResult != VmShutdownResult.Successful;
}