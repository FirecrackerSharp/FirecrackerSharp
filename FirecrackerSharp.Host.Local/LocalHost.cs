namespace FirecrackerSharp.Host.Local;

public static class LocalHost
{
    public static void Configure()
    {
        IHostFilesystem.Current = new LocalHostFilesystem();
        IHostProcessManager.Current = new LocalHostProcessManager();
    }
}