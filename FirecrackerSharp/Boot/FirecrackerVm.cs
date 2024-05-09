using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;
using Serilog;

namespace FirecrackerSharp.Boot;

public abstract class FirecrackerVm(
    VmConfiguration vmConfiguration,
    FirecrackerInstall firecrackerInstall,
    FirecrackerOptions firecrackerOptions,
    string vmId)
{
    private static readonly ILogger Logger = Log.ForContext(typeof(FirecrackerVm));
    
    protected VmConfiguration VmConfiguration = vmConfiguration;
    protected readonly FirecrackerInstall FirecrackerInstall = firecrackerInstall;
    protected readonly FirecrackerOptions FirecrackerOptions = firecrackerOptions;
    protected string? SocketPath;
    protected readonly string VmId = vmId;
    
    protected Process? Process;

    private HttpClient? _backingSocketHttpClient;
    public HttpClient SocketHttpClient
    {
        get
        {
            return _backingSocketHttpClient ??= new HttpClient(new SocketsHttpHandler
            {
                ConnectCallback = async (_, token) =>
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                    var endpoint = new UnixDomainSocketEndPoint(SocketPath!);
                    await socket.ConnectAsync(endpoint, token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
            })
            {
                BaseAddress = new Uri("http://localhost")
            };
        }
    }

    internal abstract Task StartProcessAsync();

    public abstract void CleanupAfterShutdown();

    protected async Task SerializeConfigToFileAsync(string configPath)
    {
        var configJson = JsonSerializer.Serialize(VmConfiguration, InternalUtil.SerializerOptions);
        await File.WriteAllTextAsync(configPath, configJson);
        
        Logger.Debug("Configuration was serialized (to JSON) as a transit to: {configPath}", configPath);
    }

    protected async Task WaitForBootAsync()
    {
        if (FirecrackerOptions.WaitSecondsAfterBoot.HasValue)
        {
            await Task.Delay(FirecrackerOptions.WaitSecondsAfterBoot.Value * 1000);
        }
    }

    public async Task ShutdownAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
        File.Delete(SocketPath!);
        
        try
        {
            await Process!.StandardInput.WriteLineAsync("reboot");
            try
            {
                await Process.WaitForExitAsync(cancellationTokenSource.Token);
                Logger.Information("microVM {vmId} exited gracefully", VmId);
            }
            catch (Exception)
            {
                Process.Kill();
                Logger.Warning("microVM {vmId} had to be forcefully killed", VmId);
            }
        }
        catch (Exception)
        {
            Logger.Warning("microVM {vmId} prematurely shut down", VmId);
        }
        
        CleanupAfterShutdown();
        Log.Information("microVM {vmId} was successfully cleaned up after shutdown (socket/jail deleted)", VmId);
    }
}