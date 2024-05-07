using FirecrackerSharp.Install;

var installer = new FirecrackerInstaller("/home/kanpov/Documents/firecracker");
await installer.InstallAsync();
