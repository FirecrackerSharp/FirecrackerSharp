using Serilog;

namespace FirecrackerSharp.Host.Local;

public static class LocalHost
{
    public static void Configure()
    {
        IHostFilesystem.Current = new LocalHostFilesystem();
        IHostProcessManager.Current = new LocalHostProcessManager();
        
        Log.Information("Using local (Linux) host for FirecrackerSharp");
    }
}