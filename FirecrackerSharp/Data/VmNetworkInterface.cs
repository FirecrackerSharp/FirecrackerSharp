namespace FirecrackerSharp.Data;

public sealed record VmNetworkInterface(
    string HostDevName,
    string IfaceId,
    string? GuestMac = null,
    VmRateLimiter? RxRateLimiter = null,
    VmRateLimiter? TxRateLimiter = null);
