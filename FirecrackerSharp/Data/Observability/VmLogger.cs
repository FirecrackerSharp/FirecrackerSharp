namespace FirecrackerSharp.Data.Observability;

public record VmLogger(
    string LogPath,
    VmLogLevel Level = VmLogLevel.Info,
    bool ShowLevel = false,
    bool ShowLogOrigin = false,
    string? Module = null);
