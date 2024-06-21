namespace FirecrackerSharp.Tty.Multiplexing;

public interface ICapturePathGenerator
{
    string GetFilePath(long sessionId, long commandId, string commandText, DateTimeOffset currentTime);

    internal static readonly ICapturePathGenerator Default = new DefaultCapturePathGenerator();
    
    private sealed class DefaultCapturePathGenerator : ICapturePathGenerator
    {
        public string GetFilePath(long sessionId, long commandId, string commandText, DateTimeOffset currentTime)
        {
            return $"/tmp/c_{sessionId}_{commandId}";
        }
    }
}