using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

public class StandardFirecrackerVm : FirecrackerVm
{
    private static readonly ILogger Logger = Log.ForContext(typeof(StandardFirecrackerVm));

    private StandardFirecrackerVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions) : base(vmConfiguration, firecrackerInstall, firecrackerOptions) {}
    
    internal override async Task StartProcessAsync()
    {
        var configPath = Path.GetTempFileName();
        await SerializeConfigToFileAsync(configPath);

        var args = FirecrackerOptions.FormatToArguments(configPath, SocketPath);
        Logger.Debug("Launch arguments for microVM {vmId} (unrestricted) are: {args}", VmId, args);
        Process = InternalUtil.RunProcess(FirecrackerInstall.FirecrackerBinary, args);

        await WaitForBootAsync();
        Logger.Information("Launched microVM {vmId} (unrestricted)", VmId);
    }

    public static async Task<FirecrackerVm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions)
    {
        var vm = new StandardFirecrackerVm(vmConfiguration, firecrackerInstall, firecrackerOptions);
        await vm.StartProcessAsync();
        return vm;
    }
}