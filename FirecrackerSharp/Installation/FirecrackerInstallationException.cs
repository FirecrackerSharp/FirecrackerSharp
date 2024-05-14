namespace FirecrackerSharp.Installation;

/// <summary>
/// Represents an error that happened during the automated installation of Firecracker.
/// </summary>
/// <param name="message">The error message explaining what went wrong (most likely an I/O or a GitHub API issue)</param>
public class FirecrackerInstallationException(string message) : Exception(message);
