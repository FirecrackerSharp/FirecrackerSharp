using FirecrackerSharp.Data;
using FirecrackerSharp.Host;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

public class UnrestrictedVm : Vm
{
    private static readonly ILogger Logger = Log.ForContext<UnrestrictedVm>();

    private UnrestrictedVm(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId) : base(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId)
    {
        IHostFilesystem.Current.CreateDirectory(firecrackerOptions.SocketDirectory);
        SocketPath = IHostFilesystem.Current.JoinPaths(firecrackerOptions.SocketDirectory, firecrackerOptions.SocketFilename + ".sock");
        
        Logger.Debug("The Unix socket for the unrestricted microVM will be created at: {socketPath}", SocketPath);
    }
    
    internal override async Task StartProcessAsync()
    {
        var configPath = IHostFilesystem.Current.GetTemporaryFilename();
        await SerializeConfigToFileAsync(configPath);

        var args = FirecrackerOptions.FormatToArguments(configPath, SocketPath);
        Logger.Debug("Launch arguments for microVM {vmId} (unrestricted) are: {args}", VmId, args);
        Process = IHostProcessManager.Current.LaunchProcess(FirecrackerInstall.FirecrackerBinary, args);

        await WaitForBootAsync();
        Logger.Information("Launched microVM {vmId} (unrestricted)", VmId);
    }

    protected override void CleanupAfterShutdown()
    {
        IHostFilesystem.Current.DeleteFile(SocketPath!);
    }

    public static async Task<Vm> StartAsync(
        VmConfiguration vmConfiguration,
        FirecrackerInstall firecrackerInstall,
        FirecrackerOptions firecrackerOptions,
        string vmId)
    {
        var vm = new UnrestrictedVm(vmConfiguration, firecrackerInstall, firecrackerOptions, vmId);
        await vm.StartProcessAsync();
        return vm;
    }
}