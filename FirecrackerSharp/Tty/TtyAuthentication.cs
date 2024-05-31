namespace FirecrackerSharp.Tty;

/// <summary>
/// Represents the settings needed to authenticate to the TTY of a microVM. It is highly recommended to simply enable
/// autologin to ttyS0 (or whichever one you use) during the setup of the rootfs and simply not bother with authentication,
/// however, if that's not possible, use this feature instead.
/// </summary>
/// <param name="Password">The password used to authenticate</param>
/// <param name="Username">The username used to authenticate, if <see cref="UsernameAutofilled"/> isn't true</param>
/// <param name="UsernameAutofilled">Whether the username is automatically filled by the TTY, usually false</param>
/// <param name="TimeoutSeconds">The timeout in seconds, after which to cancel the attempt to authenticate</param>
public record TtyAuthentication(
    string Password,
    string Username = "root",
    bool UsernameAutofilled = false,
    uint TimeoutSeconds = 5);
