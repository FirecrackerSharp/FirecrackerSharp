using FirecrackerSharp.Host.Ssh;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tests.Helpers;
using Renci.SshNet;

namespace FirecrackerSharp.Tests;

public class M
{
    [Fact]
    public async Task Test()
    {
        SshHost.Configure(
            new ConnectionPoolConfiguration(
                null!,
                2, 2, TimeSpan.FromSeconds(1)),
            CurlConfiguration.Default,
            ShellConfiguration.Default);

        var vm = VmArrange.GetUnrestrictedVm();
        vm.Lifecycle.AttachAllLogTargetsToSingle(ILogTarget.ToFile("/tmp/log.txt"));
        await vm.BootAsync();
        await Task.Delay(2000);
        var res = await vm.TtyClient.RunBufferedCommandAsync("cat --help");
        
        await vm.ShutdownAsync();
        
        SshHost.Dispose();
    }
}