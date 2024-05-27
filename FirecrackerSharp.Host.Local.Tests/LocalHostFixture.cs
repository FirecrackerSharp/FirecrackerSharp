namespace FirecrackerSharp.Host.Local.Tests;

public class LocalHostFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        LocalHost.Configure();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}