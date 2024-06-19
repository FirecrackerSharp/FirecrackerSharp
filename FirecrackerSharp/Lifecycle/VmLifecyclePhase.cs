namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// The phases of the microVM's lifecycle, these can be entered or exited aside from edge phases (e.g. the first and the
/// last one). The full sequence is (where NO signals the end and YES signals the start):
/// not booted NO, booting YES, booting NO, active YES, active NO, shutting down YES, shutting down NO, powered off YES.
/// </summary>
public enum VmLifecyclePhase
{
    /// <summary>
    /// The microVM has not been booted up yet. Typically, this phase would be used in order to configure lifecycle
    /// logging before booting up the microVM. Can only be exited.
    /// </summary>
    NotBooted,
    /// <summary>
    /// The microVM is being booted up.
    /// </summary>
    Booting,
    /// <summary>
    /// The microVM is being used (e.g. all actions between boot and shutdown).
    /// </summary>
    Active,
    /// <summary>
    /// The microVM is being shut down.
    /// </summary>
    ShuttingDown,
    /// <summary>
    /// The microVM has been powered off. Can only be entered.
    /// </summary>
    PoweredOff
}