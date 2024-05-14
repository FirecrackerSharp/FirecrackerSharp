namespace FirecrackerSharp.Management;

/// <summary>
/// The type of any response from the Firecracker Management API that is received through this SDK.
/// </summary>
public enum ManagementResponseType
{
    /// <summary>
    /// A successful response either with no content or encapsulating some content that was deserialized from JSON.
    /// </summary>
    Success,
    /// <summary>
    /// A failure response indicating an error made by the API user (therefore, the SDK user).
    /// </summary>
    BadRequest,
    /// <summary>
    /// A failure response indicating an unexpected error that is the API server's fault.
    /// </summary>
    InternalError
}