using FirecrackerSharp.Boot;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Shells;

namespace FirecrackerSharp.Demo;

public class StressTester(FirecrackerInstall firecrackerInstall, VmConfiguration testConfig)
{
    private readonly List<Vm> _vms = [];
    
    public async Task StartVm()
    {
        var vm = await UnrestrictedVm.StartAsync(
            testConfig, firecrackerInstall,
            new FirecrackerOptions(Guid.NewGuid().ToString()),
            //new JailerOptions(1000, 1000, "495762"),
            vmId: Random.Shared.NextInt64(100000).ToString());
        _vms.Add(vm);

        var getResp = await vm.Management.GetBalloonAsync();
        Console.WriteLine(getResp.TryUnwrap<VmBalloon>());

        var patchResp = await vm.Management.UpdateBalloonAsync(new VmBalloonUpdate(512));
        
        var getResp2 = await vm.Management.GetBalloonAsync();
        Console.WriteLine(getResp2.TryUnwrap<VmBalloon>());

        // var shell = await vm.ShellManager.StartShellAsync();
        //
        // var tasks = new List<Task>();
        // for (var i = 0; i < 1; ++i)
        // {
        //     tasks.Add(SubTask(shell));
        // }
        //
        // await Task.WhenAll(tasks);
        // await shell.QuitAsync();
    }

    private async Task SubTask(VmShell shell)
    {
        await using var command = await shell.StartCommandAsync("echo \"yay\"", CaptureMode.StdoutPlusStderr);
        Console.WriteLine(await command.CaptureOutputAsync());
    }

    public async Task ShutdownVms()
    {
        await Task.WhenAll(_vms.Select(x => x.ShutdownAsync()));
    }
}