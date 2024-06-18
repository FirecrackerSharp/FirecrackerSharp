using System.Text.Json;
using FirecrackerSharp.Host;
using Octokit;

namespace FirecrackerSharp.Installation;

/// <summary>
/// An automated tool to manage multiple simultaneous installations of Firecracker. This is the recommended approach
/// for facilitating any Firecracker environment, as it ensures compability with multiple installations and can retrieve
/// the latest versions of Firecracker (which is likely more up-to-date than the one provided by your distribution,
/// unless you use AWS).
///
/// To persist metadata about multiple installations, a JSON index file is used internally with no restrictions on
/// how many concurrent installations it can encapsulate.
/// </summary>
/// <param name="storagePath">The absolute path to the directory, which contains both the index file and all
/// installations that were created via this <see cref="FirecrackerInstallManager"/></param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> that are used to save and load the
/// index file</param>
/// <param name="indexFilename">The filename of the index</param>
public sealed class FirecrackerInstallManager(
    string storagePath,
    JsonSerializerOptions jsonSerializerOptions,
    string indexFilename = "index.json")
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private string IndexPath => IHostFilesystem.Current.JoinPaths(storagePath, indexFilename);
    
    /// <summary>
    /// An overload of the primary constructor for <see cref="FirecrackerInstallManager"/> that allows you not to
    /// specify the <see cref="JsonSerializerOptions"/>, instead using the default ones that have all default settings
    /// other than snake case naming for data.
    /// </summary>
    /// <param name="storagePath">The absolute path to the storage directory</param>
    /// <param name="indexFilename">The index filename, "index.json" by default</param>
    public FirecrackerInstallManager(string storagePath, string indexFilename = "index.json") 
        : this(storagePath, DefaultSerializerOptions, indexFilename) {}

    /// <summary>
    /// Perform an installation of Firecracker to this <see cref="FirecrackerInstallManager"/>'s specified storage by
    /// using <see cref="FirecrackerInstaller"/> internally.
    ///
    /// This method does NOT save the acquired <see cref="FirecrackerInstall"/> to the index! To do this, call
    /// <see cref="AddToIndexAsync"/> with the returned <see cref="FirecrackerInstall"/> value.
    /// </summary>
    /// <param name="releaseTag">The release tag to be installed, "latest" means the latest one available</param>
    /// <param name="repoOwner">The GitHub repository owner</param>
    /// <param name="repoName">The GitHub repository name</param>
    /// <param name="githubCredentials">The <see cref="Credentials"/> used to authorize to GitHub API in accordance with
    /// Octokit.NET. It's highly recommended to specify this in order to avoid getting rate limited for API requests made
    /// during Firecracker installation.</param>
    /// <returns>The acquired <see cref="FirecrackerInstall"/></returns>
    public async Task<FirecrackerInstall> InstallAsync(
        string releaseTag = "latest", string repoOwner = "firecracker-microvm", string repoName = "firecracker",
        Credentials? githubCredentials = null)
    {
        var installer = new FirecrackerInstaller(storagePath, releaseTag, repoOwner, repoName, githubCredentials);
        return await installer.InstallAsync();
    }
    
    /// <summary>
    /// Add a given <see cref="FirecrackerInstall"/> to the index file (duplicates are allowed).
    /// </summary>
    /// <param name="addedInstall">The <see cref="FirecrackerInstall"/> to be added.</param>
    public async Task AddToIndexAsync(FirecrackerInstall addedInstall)
    {
        if (!File.Exists(IndexPath))
        {
            var newInstalls = new List<FirecrackerInstall> { addedInstall };
            var newIndexJson = JsonSerializer.Serialize(newInstalls, jsonSerializerOptions);
            await IHostFilesystem.Current.WriteTextFileAsync(IndexPath, newIndexJson);
            return;
        }
        
        var installs = await GetAllFromIndexAsync();
        installs.Add(addedInstall);
        await WriteIndexAsync(installs);
    }

    /// <summary>
    /// Add several <see cref="FirecrackerInstall"/>'s to the index file.
    /// </summary>
    /// <param name="addedInstalls">An <see cref="IEnumerable{T}"/> with all <see cref="FirecrackerInstall"/>s to be added</param>
    public async Task AddAllToIndexAsync(IEnumerable<FirecrackerInstall> addedInstalls)
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

    /// <summary>
    /// Retrieve all <see cref="FirecrackerInstall"/>s currently stored in the index file.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> with all <see cref="FirecrackerInstall"/>s from the index</returns>
    public async Task<List<FirecrackerInstall>> GetAllFromIndexAsync()
    {
        var indexJson = await IHostFilesystem.Current.ReadTextFileAsync(IndexPath);
        return JsonSerializer.Deserialize<List<FirecrackerInstall>>(indexJson, jsonSerializerOptions)!;
    }

    /// <summary>
    /// Retrieve a <see cref="FirecrackerInstall"/> with the given version from the index file, if such a <see cref="FirecrackerInstall"/>
    /// is present.
    /// </summary>
    /// <param name="version">The version to match for</param>
    /// <param name="strict">Whether to match for exactly this version, or to use this version as a substring</param>
    /// <returns>The fetched <see cref="FirecrackerInstall"/> or null if none was found</returns>
    public async Task<FirecrackerInstall?> GetFromIndexAsync(string version, bool strict = false)
    {
        var installs = await GetAllFromIndexAsync();
        return FindInIndex(installs, version, strict).FirstOrDefault();
    }

    /// <summary>
    /// Retrieve the latest <see cref="FirecrackerInstall"/> from the index file, if the index file contains any
    /// installs.
    /// </summary>
    /// <returns>The latest <see cref="FirecrackerInstall"/> or null if none are present</returns>
    public async Task<FirecrackerInstall?> GetLatestFromIndexAsync()
    {
        var installs = await GetAllFromIndexAsync();
        return installs.MaxBy(i => new SemanticVersioning.Version(i.Version, loose: true));
    }

    /// <summary>
    /// Check whether an update can be installed to the latest available <see cref="FirecrackerInstall"/> in the index
    /// file. If there are no <see cref="FirecrackerInstall"/>s, true is returned.
    /// </summary>
    /// <param name="repoOwner">The GitHub repository owner</param>
    /// <param name="repoName">The GitHub repository name</param>
    /// <returns>Whether an update is available</returns>
    public async Task<bool> CheckForUpdatesAsync(string repoOwner = "firecracker-microvm", string repoName = "firecracker")
    {
        var currentLatestInstall = await GetLatestFromIndexAsync();
        if (currentLatestInstall is null) return false;
        
        var installer = new FirecrackerInstaller(storagePath, "latest", repoOwner, repoName);
        return await installer.CheckForUpdatesAsync(currentLatestInstall.Version);
    }

    /// <summary>
    /// Remove all occurrences of a <see cref="FirecrackerInstall"/> with the given version from the index file.
    /// </summary>
    /// <param name="version">The version to match for</param>
    /// <param name="strict">Whether to match for exactly this version, or to use this version as a substring</param>
    public async Task RemoveFromIndexAsync(string version, bool strict = false)
    {
        var installs = await GetAllFromIndexAsync();
        installs.RemoveAll(x => strict ? x.Version == version : x.Version.Contains(version));
        await WriteIndexAsync(installs);
    }

    /// <summary>
    /// Remove all <see cref="FirecrackerInstall"/>s stored in the index file.
    /// </summary>
    public async Task RemoveAllFromIndexAsync()
    {
        await WriteIndexAsync([]);
    }

    private async Task WriteIndexAsync(IEnumerable<FirecrackerInstall> installs)
    {
        var json = JsonSerializer.Serialize(installs, jsonSerializerOptions);
        await IHostFilesystem.Current.WriteTextFileAsync(IndexPath, json);
    }

    private static IEnumerable<FirecrackerInstall> FindInIndex(IEnumerable<FirecrackerInstall> installs, string version, bool strict = false)
    {
        return strict
            ? installs.Where(x => x.Version == version)
            : installs.Where(x => x.Version.Contains(version));
    }
}