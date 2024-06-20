namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// Convenience extension functions for <see cref="VmBootResult"/>s.
/// </summary>
public static class VmBootResultExtensions
{
    /// <summary>
    /// Checks if this <see cref="VmBootResult"/> is successful.
    /// </summary>
    /// <param name="bootResult">The checked <see cref="VmBootResult"/></param>
    /// <returns>The check outcome</returns>
    public static bool IsSuccessful(this VmBootResult bootResult) => bootResult == VmBootResult.Successful;

    /// <summary>
    /// Checks if this <see cref="VmBootResult"/> is a failure.
    /// </summary>
    /// <param name="bootResult">The checked <see cref="VmBootResult"/></param>
    /// <returns>The check outcome</returns>
    public static bool IsFailure(this VmBootResult bootResult) => bootResult != VmBootResult.Successful;
}