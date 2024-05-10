using System.Text.Json;
using FirecrackerSharp.Transport;

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
    private string IndexPath => IFirecrackerTransport.Current.JoinPaths(storagePath, indexFilename);
    
    public FirecrackerInstallManager(string storagePath, string indexFilename = "index.json") 
        : this(storagePath, DefaultSerializerOptions, indexFilename) {}

    public async Task<FirecrackerInstall> InstallAsync(string releaseTag = "latest",
        string repoOwner = "firecracker-microvm", string repoName = "firecracker")
    {
        var installer = new FirecrackerInstaller(storagePath, releaseTag, repoOwner, repoName);
        return await installer.InstallAsync();
    }
    
    public async Task AddToIndexAsync(FirecrackerInstall addedInstall)
    {
        if (!File.Exists(IndexPath))
        {
            var newInstalls = new List<FirecrackerInstall> { addedInstall };
            var newIndexJson = JsonSerializer.Serialize(newInstalls, jsonSerializerOptions);
            await IFirecrackerTransport.Current.WriteTextFileAsync(IndexPath, newIndexJson);
            return;
        }
        
        var installs = await GetAllFromIndexAsync();
        installs.Add(addedInstall);
        await WriteIndexAsync(installs);
    }

    public async Task AddManyToIndexAsync(IEnumerable<FirecrackerInstall> addedInstalls)
    {
        if (!File.Exists(IndexPath))
        {
            await WriteIndexAsync(addedInstalls);
            return;
        }

        var installs = await GetAllFromIndexAsync();
        installs.AddRange(addedInstalls);
        await WriteIndexAsync(installs);
    }

    public async Task<List<FirecrackerInstall>> GetAllFromIndexAsync()
    {
        var indexJson = await IFirecrackerTransport.Current.ReadTextFileAsync(IndexPath);
        return JsonSerializer.Deserialize<List<FirecrackerInstall>>(indexJson, jsonSerializerOptions)!;
    }

    public async Task<FirecrackerInstall?> GetFromIndexAsync(string version, bool strict = false)
    {
        var installs = await GetAllFromIndexAsync();
        return FindInIndex(installs, version, strict);
    }

    public async Task<bool> RemoveFromIndexAsync(string version, bool strict = false)
    {
        var installs = await GetAllFromIndexAsync();
        var install = FindInIndex(installs, version, strict);
        if (install is null) return false;
        
        installs.Remove(install);
        await WriteIndexAsync(installs);
        return true;
    }

    public async Task RemoveAllFromIndexAsync()
    {
        await WriteIndexAsync([]);
    }

    private async Task WriteIndexAsync(IEnumerable<FirecrackerInstall> installs)
    {
        var json = JsonSerializer.Serialize(installs, jsonSerializerOptions);
        await IFirecrackerTransport.Current.WriteTextFileAsync(IndexPath, json);
    }

    private static FirecrackerInstall? FindInIndex(IEnumerable<FirecrackerInstall> installs, string version, bool strict = false)
    {
        return strict
            ? installs.FirstOrDefault(x => x.Version == version)
            : installs.FirstOrDefault(x => x.Version.Contains(version));
    }
}