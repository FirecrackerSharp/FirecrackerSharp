namespace FirecrackerSharp.Data;

public record VmNetworkInterfaceUpdate(
    string IfaceId,
    VmRateLimiter RxRateLimiter,
    VmRateLimiter TxRateLimiter);
