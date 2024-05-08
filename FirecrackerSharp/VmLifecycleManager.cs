using System.Text.Json;
using FirecrackerSharp.Data;
using FirecrackerSharp.Installation;

namespace FirecrackerSharp;

public class VmLifecycleManager(
    VmConfiguration configuration,
    FirecrackerInstall firecrackerInstall,
    string socketDirectory = "/tmp/firecracker/sockets")
{
    private readonly string _socketPath = Path.Join(socketDirectory, Guid.NewGuid() + ".sock");
    
    public async Task BootWithoutJailerAsync()
    {
        var configPath = await PrepareForBootAsync();
        var proc = firecrackerInstall.RunFirecracker(
            $"--config-file \"{configPath}\" --api-sock \"{_socketPath}\"");
        Console.WriteLine(await proc.StandardOutput.ReadToEndAsync());
    }

    private async Task<string> PrepareForBootAsync()
    {
        var configPath = Path.GetTempFileName();
        var configJson = JsonSerializer.Serialize(configuration, InternalUtil.SerializerOptions);
        await File.WriteAllTextAsync(configPath, configJson);

        if (!Directory.Exists(socketDirectory))
        {
            Directory.CreateDirectory(socketDirectory);
        }
        
        return configPath;
    }
}