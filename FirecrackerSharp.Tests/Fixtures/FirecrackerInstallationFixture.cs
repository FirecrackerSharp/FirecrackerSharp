using FirecrackerSharp.Host.Local;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Tests.Fixtures;

public class FirecrackerInstallationFixture : IAsyncLifetime
{
    protected readonly string DirectoryName;
    protected readonly FirecrackerInstallManager InstallManager;

    protected FirecrackerInstallationFixture(string directoryName)
    {
        DirectoryName = directoryName;
        InstallManager = new FirecrackerInstallManager(directoryName);
    }
    
    public Task InitializeAsync()
    {
        LocalHost.Configure();
        Directory.CreateDirectory(DirectoryName);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(DirectoryName, recursive: true);
        return Task.CompletedTask;
    }
}