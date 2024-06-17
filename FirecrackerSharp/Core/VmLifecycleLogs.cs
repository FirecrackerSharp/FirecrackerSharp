namespace FirecrackerSharp.Core;

public class VmLifecycleLogs
{
    public ILogTarget BootLogTarget { internal get; set; } = ILogTarget.Null;
}