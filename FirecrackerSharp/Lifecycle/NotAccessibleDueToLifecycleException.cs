namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// An exception that is raised when a microVM's resource is attempted to be accessed that is not accessible under
/// the current <see cref="VmLifecyclePhase"/> of this microVM.
/// </summary>
/// <param name="message">The message detailing what exactly the issue was</param>
public sealed class NotAccessibleDueToLifecycleException(string message) : Exception(message);