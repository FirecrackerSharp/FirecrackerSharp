namespace FirecrackerSharp.Data.Drives;

public record VmDriveUpdate(
    string DriveId,
    string? PathOnHost = null,
    VmRateLimiter? RateLimiter = null);