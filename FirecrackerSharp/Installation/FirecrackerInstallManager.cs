using System.Text.Json;

namespace FirecrackerSharp.Installation;

public class FirecrackerInstallManager(
    string storagePath,
    JsonSerializerOptions jsonSerializerOptions,
    string indexFilename = "index.json")
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private string IndexPath => Path.Join(storagePath, indexFilename);
    
    public FirecrackerInstallManager(string storagePath, string indexFilename = "index.json") 
        : this(storagePath, DefaultSerializerOptions, indexFilename) {}

    public async Task<FirecrackerInstall> InstallToStorageAsync(string? releaseTag = null,
        string repoOwner = "firecracker-microvm", string repoName = "firecracker")
    {
        var installer = new FirecrackerInstaller(storagePath, releaseTag, repoOwner, repoName);
        return await installer.InstallAsync();
    }
    
    public async Task AddToIndexAsync(FirecrackerInstall firecrackerInstall)
    {
        if (!File.Exists(IndexPath))
        {
            var newInstalls = new List<FirecrackerInstall> { firecrackerInstall };
            var newIndexJson = JsonSerializer.Serialize(newInstalls, jsonSerializerOptions);
            await File.WriteAllTextAsync(IndexPath, newIndexJson);
            return;
        }

        var indexJson = await File.ReadAllTextAsync(IndexPath);
        var installs = JsonSerializer.Deserialize<List<FirecrackerInstall>>(indexJson, jsonSerializerOptions)!;
        installs.Add(firecrackerInstall);
        var addedIndexJson = JsonSerializer.Serialize(installs, jsonSerializerOptions);
        await File.WriteAllTextAsync(IndexPath, addedIndexJson);
    }
}