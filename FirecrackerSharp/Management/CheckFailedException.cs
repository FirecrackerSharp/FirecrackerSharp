namespace FirecrackerSharp.Management;

/// <summary>
/// An exception that is thrown when a force check of a response fails (e.g. expected success, got failure or vice versa).
/// </summary>
/// <param name="message">The detailed message describing the exact failed assertion</param>
public sealed class CheckFailedException(string message) : Exception(message);