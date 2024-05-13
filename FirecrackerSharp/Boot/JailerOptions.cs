namespace FirecrackerSharp.Boot;

public record JailerOptions(
    ulong LinuxGid,
    ulong LinuxUid,
    string? SudoPassword = null,
    string ChrootBaseDirectory = "/srv/jailer",
    string ExtraArguments = "")
{
    internal string FormatToArguments(string firecrackerBinary, string vmId)
    {
        return
            $"--exec-file {firecrackerBinary} --id {vmId} --gid {LinuxGid} --uid {LinuxUid} --chroot-base-dir {ChrootBaseDirectory} {ExtraArguments}";
    }
}