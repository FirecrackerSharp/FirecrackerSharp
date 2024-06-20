namespace FirecrackerSharp.Management;

/// <summary>
/// The type of the Management API response
/// </summary>
public enum ResponseType
{
    /// <summary>
    /// Complete success
    /// </summary>
    Success,
    /// <summary>
    /// Error due to a bad request (the SDK user's fault)
    /// </summary>
    BadRequest,
    /// <summary>
    /// Error due to an internal error (Firecracker's fault)
    /// </summary>
    InternalError
}