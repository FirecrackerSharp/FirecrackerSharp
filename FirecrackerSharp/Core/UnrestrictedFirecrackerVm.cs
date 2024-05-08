using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Core;

public class UnrestrictedFirecrackerVm(
    VmConfiguration vmConfiguration,
    FirecrackerInstall firecrackerInstall,
    FirecrackerOptions firecrackerOptions,
    int? bootDelay = 2) : FirecrackerVm(vmConfiguration, firecrackerInstall, firecrackerOptions)
{
    private static readonly ILogger Logger = Log.ForContext(typeof(UnrestrictedFirecrackerVm));
    
    internal override async Task StartProcessAsync()
    {
        var configPath = Path.GetTempFileName();
        await SerializeConfigToFileAsync(configPath);
        
        var args = $"--config-file {configPath} --api-sock {SocketPath} {FirecrackerOptions.ExtraArguments}";
        Logger.Debug("Launch arguments for microVM {vmId} (unrestricted) are: {args}", VmId, args);
        Process = FirecrackerInstall.RunFirecracker(args);

        if (bootDelay.HasValue)
        {
            await Task.Delay(bootDelay.Value * 1000);
        }
    }

    public static async Task<FirecrackerVm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions)
    {
        var vm = new UnrestrictedFirecrackerVm(vmConfiguration, firecrackerInstall, firecrackerOptions);
        await vm.StartProcessAsync();
        return vm;
    }
}