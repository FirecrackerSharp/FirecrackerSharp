using FirecrackerSharp.Host.Local;

namespace FirecrackerSharp.Tests;

public class GenericFixture : IAsyncLifetime
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