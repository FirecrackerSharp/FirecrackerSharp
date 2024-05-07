using System.Security.Cryptography;
using System.Text;
using Octokit;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FirecrackerSharp.Install;

public class FirecrackerInstaller(
    string installRoot,
    string? releaseTag = null,
    string repoOwner = "firecracker-microvm",
    string repoName = "firecracker")
{
    private static readonly HttpClient HttpClient = new();
    
    public async Task<FirecrackerInstall> InstallAsync()
    {
        var (archiveAsset, archiveChecksumAsset) = await FetchAssetsFromApiAsync();
        var installDirectory = Path.Join(installRoot, releaseTag + Guid.NewGuid());
        Directory.CreateDirectory(installDirectory);
        var archivePath = await DownloadAssetsAndVerifyAsync(archiveAsset, archiveChecksumAsset);
        var install = await ExtractToInstallRootAsync(archivePath, installDirectory);

        return install;
    }

    private async Task<(ReleaseAsset, ReleaseAsset)> FetchAssetsFromApiAsync()
    {
        var githubClient = new GitHubClient(new ProductHeaderValue("FirecrackerSharp"));
        var repository = await githubClient.Repository.Get(repoOwner, repoName);
        if (repository is null)
        {
            throw new FirecrackerInstallException(
                $"Could not the Firecracker GitHub repository owned by {repoOwner} and named {repoName}");
        }

        var releases = await githubClient.Repository.Release.GetAll(repository.Id);
        if (releases is null)
        {
            throw new FirecrackerInstallException("Could not query Firecracker releases on the specified repository");
        }

        var release = releaseTag is null
            ? releases.MaxBy(x => x.CreatedAt)
            : releases.FirstOrDefault(x => x.TagName == releaseTag);
        if (release is null)
        {
            throw new FirecrackerInstallException(
                releaseTag is null ? "Could not find the latest release" : $"Could not find the release with the tag {releaseTag}");
        }

        var archiveAsset = release.Assets.FirstOrDefault(x => x.Name.EndsWith("x86_64.tgz"));
        var archiveChecksumAsset = release.Assets.FirstOrDefault(x => x.Name.EndsWith("x86_64.tgz.sha256.txt"));
        if (archiveAsset is null || archiveChecksumAsset is null)
        {
            throw new FirecrackerInstallException("Could not fetch the necessary x86_64 archive and checksum");
        }

        return (archiveAsset, archiveChecksumAsset);
    }

    private async Task<FirecrackerInstall> ExtractToInstallRootAsync(string archivePath, string installDirectory)
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory().FullName;

        await using (Stream stream = File.OpenRead(archivePath))
        {            
            var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                    reader.WriteEntryToDirectory(temporaryDirectory,
                        new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            }
        }

        var subdirectoryPath = Directory.GetDirectories(temporaryDirectory).FirstOrDefault();
        if (subdirectoryPath is null)
        {
            throw new FirecrackerInstallException("The extracted Firecracker directory doesn't contain the" +
                                                  "expected subdirectory");
        }

        var files = Directory.GetFiles(subdirectoryPath);
        var firecrackerBinaryPath = files.First(x => x.Contains("firecracker") && !x.Contains("debug"));
        var jailerBinaryPath = files.First(x => x.Contains("jailer") && !x.Contains("debug"));
        var newFirecrackerBinaryPath = Path.Join(installDirectory, "firecracker");
        var newJailerBinaryPath = Path.Join(installDirectory, "jailer");
        File.Copy(firecrackerBinaryPath, newFirecrackerBinaryPath);
        File.Copy(jailerBinaryPath, newJailerBinaryPath);
        
        Directory.Delete(temporaryDirectory, recursive: true);
        File.Delete(archivePath);
        
        return new FirecrackerInstall(releaseTag ?? "latest", newFirecrackerBinaryPath, newJailerBinaryPath);
    }

    private static async Task<string> DownloadAssetsAndVerifyAsync(ReleaseAsset archiveAsset, ReleaseAsset archiveChecksumAsset)
    {
        var expectedChecksum = await HttpClient.GetStringAsync(archiveChecksumAsset.BrowserDownloadUrl);
        expectedChecksum = expectedChecksum.Split(" ").First();
        
        var archivePath = Path.GetTempFileName() + ".tar.gz";
        var archiveBytes = await HttpClient.GetByteArrayAsync(archiveAsset.BrowserDownloadUrl);

        var actualChecksum = ToHex(SHA256.HashData(archiveBytes));
        if (expectedChecksum != actualChecksum)
        {
            throw new FirecrackerInstallException("The actual checksum doesn't match the expected one from GitHub");
        }
        
        await File.WriteAllBytesAsync(archivePath, archiveBytes);

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