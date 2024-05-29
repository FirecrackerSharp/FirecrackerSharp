namespace FirecrackerSharp.Tty;

/// <summary>
/// An exception indicating an internal error within the SDK relating to I/O operations to a microVM's TTY
/// (failure to read or write).
/// </summary>
/// <param name="message">The message describing the error</param>
public class TtyException(string message) : Exception(message);