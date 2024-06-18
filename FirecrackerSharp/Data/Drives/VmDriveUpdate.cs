namespace FirecrackerSharp.Data.Drives;

public sealed record VmDriveUpdate(
    string DriveId,
    string? PathOnHost = null,
    VmRateLimiter? RateLimiter = null);