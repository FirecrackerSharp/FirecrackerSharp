namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// A narrowed down selection of only those <see cref="VmLifecyclePhase"/>s that can be logged to <see cref="ILogTarget"/>s.
/// </summary>
public enum LoggedVmLifecyclePhase
{
    /// <summary>
    /// The boot process of the microVM, including (when applicable) the setup of the jailer and the setup of the
    /// environment.
    /// </summary>
    Boot,
    /// <summary>
    /// The activity of the microVM, all of its usage between boot and shutdown.
    /// </summary>
    Active,
    /// <summary>
    /// The shutdown process of the microVM, including the teardown of the jailer.
    /// </summary>
    Shutdown
}