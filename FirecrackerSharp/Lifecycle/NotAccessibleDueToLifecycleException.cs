namespace FirecrackerSharp.Lifecycle;

public class NotAccessibleDueToLifecycleException(string message) : Exception(message);