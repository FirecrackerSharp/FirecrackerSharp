namespace FirecrackerSharp.Tty.CompletionTracking;

/// <summary>
/// The context forwarded to a <see cref="ICompletionTracker"/> that is shared for a given write operation.
/// </summary>
/// <param name="TtyClient">The <see cref="VmTtyClient"/> performing the write operation</param>
/// <param name="TrackingStartTime">The <see cref="DateTimeOffset"/> UTC time when the write operation started</param>
/// <param name="InputText">The transformed input text of the write operation</param>
public sealed record CompletionTrackerContext(
    VmTtyClient TtyClient,
    DateTimeOffset TrackingStartTime,
    string InputText);
