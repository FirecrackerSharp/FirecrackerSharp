using FirecrackerSharp.Installation;

var installManager = new FirecrackerInstallManager("/home/kanpov/Documents/firecracker");
var install = await installManager.InstallToStorageAsync();
await installManager.AddToIndexAsync(install);
