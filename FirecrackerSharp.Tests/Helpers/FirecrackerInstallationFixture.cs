using FirecrackerSharp.Host.Local;
using FirecrackerSharp.Installation;
using Octokit;

namespace FirecrackerSharp.Tests.Helpers;

public class FirecrackerInstallationFixture : IAsyncLifetime
{
    private readonly string _directoryName;
    protected readonly FirecrackerInstallManager InstallManager;
    protected readonly FirecrackerInstaller Installer;
    
    protected static Credentials? GithubCredentials
    {
        get
        {
            var token = Environment.GetEnvironmentVariable("FSH_GITHUB_TOKEN");
            return token is null
                ? null
                : new Credentials(token, AuthenticationType.Bearer);
        }
    }

    protected FirecrackerInstallationFixture(string directoryName)
    {
        _directoryName = directoryName;
        InstallManager = new FirecrackerInstallManager(_directoryName);
        Installer = new FirecrackerInstaller(_directoryName, githubCredentials: GithubCredentials);
    }
    
    public Task InitializeAsync()
    {
        LocalHost.Configure();
        Directory.CreateDirectory(_directoryName);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_directoryName, recursive: true);
        return Task.CompletedTask;
    }
}