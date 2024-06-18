namespace FirecrackerSharp.Tty.CompletionTracking;

public record CompletionTrackerContext(
    VmTtyClient TtyClient,
    DateTimeOffset TrackingStartTime,
    string InputText);
