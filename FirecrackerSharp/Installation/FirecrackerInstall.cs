namespace FirecrackerSharp.Installation;

/// <summary>
/// A reference to an installation of Firecracker containing the minimum binaries required for the SDK to operate:
/// the firecracker and the jailer.
///
/// For one install, retrieve this via <see cref="FirecrackerInstaller"/>, and for multiple installs use <see cref="FirecrackerInstallManager"/>
/// instead. However, if both approaches aren't sufficient, creating this object manually is also viable.
/// </summary>
/// <param name="Version">The version (GitHub tag) of this Firecracker installation</param>
/// <param name="FirecrackerBinary">The absolute path to the firecracker binary, which must be executable</param>
/// <param name="JailerBinary">The absolute path to the jailer binary, which must be executable</param>
public record FirecrackerInstall(
    string Version,
    string FirecrackerBinary,
    string JailerBinary);
