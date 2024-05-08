using FirecrackerSharp.Installation;

var installManager = new FirecrackerInstallManager("/home/kanpov/Documents/firecracker");
var install = await installManager.InstallAsync("v1.4.0");
await installManager.AddToIndexAsync(install);
