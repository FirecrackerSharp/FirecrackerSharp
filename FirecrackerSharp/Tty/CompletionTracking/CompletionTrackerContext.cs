namespace FirecrackerSharp.Tty.CompletionTracking;

public sealed record CompletionTrackerContext(
    VmTtyClient TtyClient,
    DateTimeOffset TrackingStartTime,
    string InputText);
