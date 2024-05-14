using Serilog;

namespace FirecrackerSharp.Host.Local;

/// <summary>
/// An SDK Linux host that is a local Linux machine.
///
/// This host offers by far the best performance as it eliminates the overhead of either containerization (Podman) or
/// communication to a remote host through the network (SSH). This is why we highly recommend using the local host for
/// any production needs unless remote communication is strictly necessary!
/// </summary>
public static class LocalHost
{
    /// <summary>
    /// Configure the local host to be used internally by the SDK.
    /// </summary>
    public static void Configure()
    {
        IHostFilesystem.Current = new LocalHostFilesystem();
        IHostProcessManager.Current = new LocalHostProcessManager();
        IHostSocketManager.Current = new LocalHostSocketManager();  
        
        Log.Information("Using local (Linux) host for FirecrackerSharp");
    }
}