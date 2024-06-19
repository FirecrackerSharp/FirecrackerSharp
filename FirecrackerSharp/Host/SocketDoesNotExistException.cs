namespace FirecrackerSharp.Host;

public sealed class SocketDoesNotExistException(string message) : Exception(message);
