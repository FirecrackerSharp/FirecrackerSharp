using FirecrackerSharp.Host.Local;

namespace FirecrackerSharp.Tests.Helpers;

public class MinimalFixture : IAsyncLifetime
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