using Serilog;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FirecrackerSharp.Host.Local;

internal class LocalHostFilesystem : IHostFilesystem
{
    public async Task WriteTextFileAsync(string path, string content)
    {
        await File.WriteAllTextAsync(path, content);
    }

    public async Task WriteBinaryFileAsync(string path, byte[] content)
    {
        await File.WriteAllBytesAsync(path, content);
    }

    public async Task<string> ReadTextFileAsync(string path)
    {
        return await File.ReadAllTextAsync(path);
    }

    public async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        await using var sourceStream = File.OpenRead(sourcePath);
        await using var destinationStream = File.OpenWrite(destinationPath);
        await sourceStream.CopyToAsync(destinationStream);
    }

    public string GetTemporaryFilename()
    {
        return Path.GetTempFileName();
    }

    public bool FileOrDirectoryExists(string filename)
    {
        return File.Exists(filename);
    }

    public void CreateTextFile(string filename)
    {
        File.CreateText(filename).Close();
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public IEnumerable<string> GetSubdirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return Directory.GetFiles(path);
    }

    public async Task ExtractGzipAsync(string archivePath, string destinationPath)
    {
        await using var stream = File.OpenRead(archivePath);

        var reader = ReaderFactory.Open(stream);
        while (reader.MoveToNextEntry())
        {
            if (!reader.Entry.IsDirectory)
            {
                reader.WriteEntryToDirectory(destinationPath,
                    new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            }
        }
    }

    public void MakeFileExecutable(string path)
    {
#pragma warning disable CA1416
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
#pragma warning restore CA1416
    }

    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public void DeleteDirectoryRecursively(string path)
    {
        Directory.Delete(path, recursive: true);
    }

    public string CreateTemporaryDirectory()
    {
        return Directory.CreateTempSubdirectory().FullName;
    }

    public string JoinPaths(params string[] paths)
    {
        return Path.Join(paths);
    }
}