namespace FirecrackerSharp.Data;

public record VmRateLimiter(
    VmTokenBucket Bandwidth,
    VmTokenBucket Ops);
