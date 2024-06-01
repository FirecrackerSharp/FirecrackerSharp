## FirecrackerSharp Local Host

A host implementation for the `FirecrackerSharp` microVM SDK that uses a native Linux environment instead of a remote
connection or any other type of bridge.

This approach is recommended for development if you can work on a Linux desktop OS, and is heavily recommended for
production in order to avoid latency and stability issues that are associated with a bridge to a non-native Linux
environment.
