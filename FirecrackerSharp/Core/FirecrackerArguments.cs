namespace FirecrackerSharp.Core;

public record FirecrackerArguments(
    bool LoadBootTimer = false,
    long? ApiMaxPayloadSize = null,
    string? MetadataFilepath = null,
    string? MetricsFilepath = null,
    long? MetadataStoreLimit = null,
    bool NoSeccompFiltering = false,
    string? SeccompFilterPath = null,
    bool ShowLevel = false,
    bool ShowLogOrigin = false)
{
    public string Format(string configPath, string socketPath, Guid vmId)
    {
        var args = $" --config-file {configPath} --api-sock {socketPath} --id {vmId}";
        if (LoadBootTimer)
            args += " --boot-timer";
        if (ApiMaxPayloadSize.HasValue)
            args += $" --http-api-max-payload-size {ApiMaxPayloadSize.Value}";
        if (MetadataFilepath != null)
            args += $" --metadata {MetadataFilepath}";
        if (MetricsFilepath != null)
            args += $" --metrics-path {MetricsFilepath}";
        if (MetadataStoreLimit.HasValue)
            args += $" --mmds-size-limit {MetadataStoreLimit.Value}";
        if (NoSeccompFiltering)
            args += " --no-seccomp";
        if (SeccompFilterPath != null)
            args += $" --seccomp-filter {SeccompFilterPath}";
        if (ShowLevel)
            args += " --show-level";
        if (ShowLogOrigin)
            args += " --show-log-origin";
        return args;
    }
}