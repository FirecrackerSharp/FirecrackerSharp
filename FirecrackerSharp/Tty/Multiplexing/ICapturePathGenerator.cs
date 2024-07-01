namespace FirecrackerSharp.Tty.Multiplexing;

public interface ICapturePathGenerator
{
    string GetCapturePath(ulong commandId, string commandText, CaptureMode captureMode);

    public static readonly ICapturePathGenerator Default = new DefaultCapturePathGenerator();

    private sealed class DefaultCapturePathGenerator : ICapturePathGenerator
    {
        public string GetCapturePath(ulong commandId, string commandText, CaptureMode captureMode)
        {
            return $"/tmp/capture-{commandId}";
        }
    }
}