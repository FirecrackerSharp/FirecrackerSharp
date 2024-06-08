using System.Security.Cryptography;
using System.Text;
using FirecrackerSharp.Host;
using Octokit;
using Serilog;

namespace FirecrackerSharp.Installation;

/// <summary>
/// An automatic installation tool for a single installation of Firecracker, which contains the firecracker binary and the
/// jailer binary.
///
/// The installation process is automated with the help of the GitHub API referencing the releases of the public
/// Firecracker GitHub repository (currently, firecracker-microvm/firecracker), however, the repository can be
/// overridden if necessary.
///
/// If you're looking to manage multiple simultaneous installations of Firecracker (as is recommended), please consider
/// using <see cref="FirecrackerInstallManager"/> instead, as it provides that capability out-of-the-box.
/// </summary>
/// <param name="installRoot">The installation root directory (the final directory is $installRoot/$version/$randomGuid)</param>
/// <param name="releaseTag">Either the GitHub tag of the needed release, or "latest" indicating the most recent available release</param>
/// <param name="repoOwner">The GitHub owner account of the repository (firecracker-microvm by default)</param>
/// <param name="repoName">The name of the GitHub repository (firecracker by default)</param>
/// <param name="githubCredentials">The <see cref="Credentials"/> used to authorize to GitHub API in accordance with
/// Octokit.NET. It's highly recommended to specify this in order to avoid getting rate limited for API requests made
/// during Firecracker installation.</param>
public class FirecrackerInstaller(
    string installRoot,
    string releaseTag = "latest",
    string repoOwner = "firecracker-microvm",
    string repoName = "firecracker",
    Credentials? githubCredentials = null)
{
    private static readonly HttpClient HttpClient = new();
    private static readonly ILogger Logger = Log.ForContext(typeof(FirecrackerInstaller));
    
    /// <summary>
    /// Asynchronously install Firecracker with the parameters specified in <see cref="FirecrackerInstaller"/>'s
    /// constructor.
    /// </summary>
    /// <returns>The <see cref="FirecrackerInstall"/> object referencing the new install, which can be used in
    /// order to interface with VM boot-up facilities of this SDk</returns>
    public async Task<FirecrackerInstall> InstallAsync()
    {
        var (archiveAsset, archiveChecksumAsset, release) = await FetchAssetsFromApiAsync();
        var fetchedReleaseTag = release.TagName;
        var installDirectory =
            IHostFilesystem.Current.JoinPaths(installRoot, fetchedReleaseTag, Guid.NewGuid().ToString());
        IHostFilesystem.Current.CreateDirectory(installDirectory);
        var archivePath = await DownloadAssetsAndVerifyAsync(archiveAsset, archiveChecksumAsset);
        var firecracker = await ExtractToInstallRootAsync(archivePath, installDirectory, fetchedReleaseTag);

        Logger.Information("Installed Firecracker {tag} to: {installDirectory}", fetchedReleaseTag, installDirectory);
        return firecracker;
    }

    /// <summary>
    /// Asynchronously check whether an update is available to a given version (release tag).
    /// </summary>
    /// <param name="currentReleaseTag">The version (release tag) to be checked against</param>
    /// <returns>Whether an update is available</returns>
    public async Task<bool> CheckForUpdatesAsync(string currentReleaseTag)
    {
        var (_, _, release) = await FetchAssetsFromApiAsync();
        var currentVersion = new SemanticVersioning.Version(currentReleaseTag, loose: true);
        var latestVersion = new SemanticVersioning.Version(release.TagName, loose: true);

        return latestVersion > currentVersion;
    }

    private async Task<(ReleaseAsset, ReleaseAsset, Release)> FetchAssetsFromApiAsync()
    {
        var githubClient = new GitHubClient(new ProductHeaderValue("FirecrackerSharp"));
        if (githubCredentials is not null)
        {
            githubClient.Credentials = githubCredentials;
        }
        
        var repository = await githubClient.Repository.Get(repoOwner, repoName);
        if (repository is null)
        {
            throw new FirecrackerInstallationException(
                $"Could not the Firecracker GitHub repository owned by {repoOwner} and named {repoName}");
        }
        Logger.Debug("Fetched GitHub repository from API");

        var releases = await githubClient.Repository.Release.GetAll(repository.Id);
        if (releases is null)
        {
            throw new FirecrackerInstallationException("Could not query Firecracker releases on the specified repository");
        }
        Logger.Debug("Fetched GitHub releases from API");

        var release = releaseTag == "latest"
            ? releases.MaxBy(x => x.CreatedAt)
            : releases.FirstOrDefault(x => x.TagName == releaseTag);
        if (release is null)
        {
            throw new FirecrackerInstallationException(
                releaseTag == "latest" ? "Could not find the latest release" : $"Could not find the release with the tag {releaseTag}");
        }

        var archiveAsset = release.Assets.FirstOrDefault(x => x.Name.EndsWith("x86_64.tgz"));
        var archiveChecksumAsset = release.Assets.FirstOrDefault(x => x.Name.EndsWith("x86_64.tgz.sha256.txt"));
        if (archiveAsset is null || archiveChecksumAsset is null)
        {
            throw new FirecrackerInstallationException("Could not fetch the necessary x86_64 archive and checksum");
        }

        return (archiveAsset, archiveChecksumAsset, release);
    }

    private static async Task<FirecrackerInstall> ExtractToInstallRootAsync(string archivePath, string installDirectory,
        string fetchedReleaseTag)
    {
        var temporaryDirectory = IHostFilesystem.Current.CreateTemporaryDirectory();
        await IHostFilesystem.Current.ExtractGzipAsync(archivePath, temporaryDirectory);
        Logger.Debug("Extracted Firecracker binaries archive");

        var subdirectoryPath = IHostFilesystem.Current
            .GetSubdirectories(temporaryDirectory)
            .FirstOrDefault();
        if (subdirectoryPath is null)
        {
            throw new FirecrackerInstallationException("The extracted Firecracker directory doesn't contain the" +
                                                  "expected subdirectory");
        }

        var files = IHostFilesystem.Current
            .GetFiles(subdirectoryPath)
            .ToList();
        var firecrackerBinaryPath = files.First(x => x.Contains("firecracker") && !x.Contains("debug"));
        var jailerBinaryPath = files.First(x => x.Contains("jailer") && !x.Contains("debug"));
        var newFirecrackerBinaryPath = IHostFilesystem.Current.JoinPaths(installDirectory, "firecracker");
        var newJailerBinaryPath = IHostFilesystem.Current.JoinPaths(installDirectory, "jailer");
        
        await Task.WhenAll([
            IHostFilesystem.Current.CopyFileAsync(firecrackerBinaryPath, newFirecrackerBinaryPath),
            IHostFilesystem.Current.CopyFileAsync(jailerBinaryPath, newJailerBinaryPath)
        ]);
        
        IHostFilesystem.Current.MakeFileExecutable(newFirecrackerBinaryPath);
        IHostFilesystem.Current.MakeFileExecutable(newJailerBinaryPath);
        
        IHostFilesystem.Current.DeleteDirectoryRecursively(temporaryDirectory);
        IHostFilesystem.Current.DeleteFile(archivePath);
        
        return new FirecrackerInstall(fetchedReleaseTag, newFirecrackerBinaryPath, newJailerBinaryPath);
    }

    private static async Task<string> DownloadAssetsAndVerifyAsync(ReleaseAsset archiveAsset, ReleaseAsset archiveChecksumAsset)
    {
        var expectedChecksum = await HttpClient.GetStringAsync(archiveChecksumAsset.BrowserDownloadUrl);
        expectedChecksum = expectedChecksum.Split(" ").First();
        
        var archivePath = Path.GetTempFileName() + ".tar.gz";
        var archiveBytes = await HttpClient.GetByteArrayAsync(archiveAsset.BrowserDownloadUrl);
        Logger.Debug("Downloaded Firecracker binaries archive");

        var actualChecksum = ToHex(SHA256.HashData(archiveBytes));
        if (expectedChecksum != actualChecksum)
        {
            throw new FirecrackerInstallationException("The actual checksum doesn't match the expected one from GitHub");
        }
        Logger.Debug("Verified checksums successfully");

        await IHostFilesystem.Current.WriteBinaryFileAsync(archivePath, archiveBytes);

        return archivePath;
    }
    
    private static string ToHex(IReadOnlyCollection<byte> bytes)
    {
        var result = new StringBuilder(bytes.Count * 2);
        foreach (var t in bytes)
            result.Append(t.ToString("x2"));

        return result.ToString();
    }
}