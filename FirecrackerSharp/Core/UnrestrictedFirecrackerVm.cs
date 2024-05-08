using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Core;

public class UnrestrictedFirecrackerVm : FirecrackerVm
{
    private static readonly ILogger Logger = Log.ForContext(typeof(UnrestrictedFirecrackerVm));
    private readonly int? _bootDelay;

    private UnrestrictedFirecrackerVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        int? bootDelay = 2) : base(vmConfiguration, firecrackerInstall, firecrackerOptions)
    {
        _bootDelay = bootDelay;
    }

    internal override async Task StartProcessAsync()
    {
        var configPath = Path.GetTempFileName();
        await SerializeConfigToFileAsync(configPath);
        
        var args = $"--config-file {configPath} --api-sock {SocketPath} {FirecrackerOptions.ExtraArguments}";
        Logger.Debug("Launch arguments for microVM {vmId} (unrestricted) are: {args}", VmId, args);
        Process = FirecrackerInstall.RunFirecracker(args);

        if (_bootDelay.HasValue)
        {
            await Task.Delay(_bootDelay.Value * 1000);
        }
        
        Logger.Information("Launched microVM {vmId} (unrestricted)", VmId);
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