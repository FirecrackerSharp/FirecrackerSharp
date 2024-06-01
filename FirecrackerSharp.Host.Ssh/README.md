## FirecrackerSharp SSH Host

A host implementation for the `FirecrackerSharp` microVM SDK that uses the `SSH.NET` library to do microVM operations
on a remote SSH machine that must have Linux installed.

**NOTE:** the SSH system must have `tar`, `untar` and `curl` installed in order for the SSH host to function correctly.
`tar` and `untar` are needed for archive management, while `curl` is used to communicate with microVM Unix domain sockets.
