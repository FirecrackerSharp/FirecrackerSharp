namespace FirecrackerSharp.Core;

public record JailerOptions(
    string JailId,
    ulong LinuxGid,
    ulong LinuxUid,
    string ChrootBaseDirectory = "/srv/jailer",
    string ExtraArguments = "");