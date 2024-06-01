## FirecrackerSharp

FirecrackerSharp is a C# SDK for the innovative Firecracker microVM hypervisor developed by Amazon. It provides
.NET developers with the ability to develop applications that utilize microVMs.

Since Firecracker depends on KVM and thus only runs on Linux, you'll need an additional package that provides the SDK
with a Linux environment:

- `FirecrackerSharp.Host.Local` - run microVMs on your own machine natively. This will require a Linux desktop OS and is
  the recommended approach for developing with the SDK.
- `FirecrackerSharp.Host.Ssh` - run microVMs over an SSH connection to a remote Linux machine. This is currently the
  only available method if you run Windows or macOS as your development environment.
