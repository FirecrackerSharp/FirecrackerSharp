namespace FirecrackerSharp.Data;

public record VmDrive(
    string DriveId,
    bool IsRootDevice,
    string? Partuuid = null,
    VmCacheType CacheType = VmCacheType.Unsafe,
    bool? IsReadOnly = null,
    string? PathOnHost = null,
    VmIoEngine IoEngine = VmIoEngine.Sync,
    string? Socket = null,
    VmRateLimiter? RateLimiter = null);