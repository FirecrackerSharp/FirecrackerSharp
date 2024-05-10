using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Transport;
using Serilog;

namespace FirecrackerSharp.Boot;

public class UnrestrictedFirecrackerVm : FirecrackerVm
{
    private static readonly ILogger Logger = Log.ForContext<UnrestrictedFirecrackerVm>();

    private UnrestrictedFirecrackerVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId) : base(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId)
    {
        IFirecrackerTransport.Current.CreateDirectory(firecrackerOptions.SocketDirectory);
        SocketPath = Path.Join(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        
        Logger.Debug("The Unix socket for the unrestricted microVM will be created at: {socketPath}", SocketPath);
    }
    
    internal override async Task StartProcessAsync()
    {
        var configPath = IFirecrackerTransport.Current.GetTemporaryFilename();
        await SerializeConfigToFileAsync(configPath);

        var args = FirecrackerOptions.FormatToArguments(configPath, SocketPath);
        Logger.Debug("Launch arguments for microVM {vmId} (unrestricted) are: {args}", VmId, args);
        Process = IFirecrackerTransport.Current.LaunchProcess(FirecrackerInstall.FirecrackerBinary, args);

        await WaitForBootAsync();
        Logger.Information("Launched microVM {vmId} (unrestricted)", VmId);
    }

    public override void CleanupAfterShutdown()
    {
        IFirecrackerTransport.Current.DeleteFile(SocketPath!);
    }

    public static async Task<FirecrackerVm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId)
    {
        var vm = new UnrestrictedFirecrackerVm(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId);
        await vm.StartProcessAsync();
        return vm;
    }
}