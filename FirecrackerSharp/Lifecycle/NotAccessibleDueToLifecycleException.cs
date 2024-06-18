namespace FirecrackerSharp.Lifecycle;

public sealed class NotAccessibleDueToLifecycleException(string message) : Exception(message);