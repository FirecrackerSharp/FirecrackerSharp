namespace FirecrackerSharp.Boot;

public record JailerOptions(
    string JailId,
    ulong LinuxGid,
    ulong LinuxUid,
    string SudoPassword,
    string ChrootBaseDirectory = "/srv/jailer",
    string ExtraArguments = "")
{
    internal string FormatToArguments(string firecrackerBinary)
    {
        return
            $"--exec-file {firecrackerBinary} --id {JailId} --gid {LinuxGid} --uid {LinuxUid} --chroot-base-dir {ChrootBaseDirectory} {ExtraArguments}";
    }
}