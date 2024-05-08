using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp.Core;

public class FirecrackerVm
{
    private readonly string _socketPath;
    private readonly FirecrackerVmConfiguration _vmConfiguration;
    private readonly FirecrackerInstall _firecrackerInstall;

    private FirecrackerVm(FirecrackerVmConfiguration vmConfiguration, FirecrackerInstall firecrackerInstall,
        string socketDirectory = "/tmp/firecracker/sockets", string? socketFilename = null)
    {
        socketFilename ??= Guid.NewGuid().ToString();
        _socketPath = Path.Join(socketDirectory, socketFilename + ".sock");
        if (!Directory.Exists(socketDirectory)) Directory.CreateDirectory(socketDirectory);
        
        _vmConfiguration = vmConfiguration;
        _firecrackerInstall = firecrackerInstall;
    }

    private async Task InternalStartAsync()
    {
        var configPath = await PrepareForBootAsync();
        var proc = _firecrackerInstall.RunFirecracker(
            $"--config-file \"{configPath}\" --api-sock \"{_socketPath}\"");

        await Task.Delay(3);
        proc.Kill();
        Console.WriteLine(await proc.StandardOutput.ReadToEndAsync());
    }

    private async Task<string> PrepareForBootAsync()
    {
        var configPath = Path.GetTempFileName();
        var configJson = JsonSerializer.Serialize(_vmConfiguration, InternalUtil.SerializerOptions);
        await File.WriteAllTextAsync(configPath, configJson);

        return configPath;
    }

    public static async Task<FirecrackerVm> StartAsync(
        FirecrackerVmConfiguration vmConfiguration, FirecrackerInstall firecrackerInstall,
        string socketDirectory = "/tmp/firecracker/sockets", string? socketFilename = null)
    {
        var firecrackerVm = new FirecrackerVm(vmConfiguration, firecrackerInstall, socketDirectory, socketFilename);
        await firecrackerVm.InternalStartAsync();
        return firecrackerVm;
    }
}