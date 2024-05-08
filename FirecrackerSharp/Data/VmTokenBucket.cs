namespace FirecrackerSharp.Data;

public record VmTokenBucket(
    int RefillTime,
    int Size,
    int? OneTimeBurst = null);