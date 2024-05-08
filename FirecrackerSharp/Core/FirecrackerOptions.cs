namespace FirecrackerSharp.Core;

public record FirecrackerOptions(
    string SocketFilename,
    string SocketDirectory = "/tmp/firecracker/sockets",
    string ExtraArguments = "",
    int? WaitSecondsAfterBoot = 2);