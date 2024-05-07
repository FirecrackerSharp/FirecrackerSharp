using System.Text.Json;

namespace FirecrackerSharp.Installation;

public class FirecrackerIndexManager(
    string storagePath,
    JsonSerializerOptions jsonSerializerOptions,
    string indexFilename = "index.json")
{
    private string IndexPath => Path.Join(storagePath, indexFilename);
    
    public FirecrackerIndexManager(string storagePath, string indexFilename = "index.json") 
        : this(storagePath, new JsonSerializerOptions(), indexFilename) {}

    public async Task AddToIndexAsync(FirecrackerInstall firecrackerInstall)
    {
        if (!File.Exists(IndexPath))
        {
            var newIndex = new FirecrackerIndex([firecrackerInstall]);
            var newIndexJson = JsonSerializer.Serialize(newIndex, jsonSerializerOptions);
            await File.WriteAllTextAsync(IndexPath, newIndexJson);
            return;
        }

        var indexJson = await File.ReadAllTextAsync(IndexPath);
        var index = JsonSerializer.Deserialize<FirecrackerIndex>(indexJson, jsonSerializerOptions)!;
        index.Installs.Add(firecrackerInstall);
        var addedIndexJson = JsonSerializer.Serialize(index, jsonSerializerOptions);
        await File.WriteAllTextAsync(IndexPath, addedIndexJson);
    }
}