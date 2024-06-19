using System.Text;

namespace FirecrackerSharp.Host.Ssh;

internal sealed class SshHostFilesystem(ConnectionPool connectionPool) : IHostFilesystem
{
    public Task WriteTextFileAsync(string path, string content)
    {
        connectionPool.Sftp.WriteAllText(path, content);
        return Task.CompletedTask;
    }

    public void AppendTextFile(string path, string content)
    {
        connectionPool.Sftp.AppendAllText(path, content);
    }

    public Task WriteBinaryFileAsync(string path, byte[] content)
    {
        connectionPool.Sftp.WriteAllBytes(path, content);
        return Task.CompletedTask;
    }

    public Task<string> ReadTextFileAsync(string path)
    {
        return Task.FromResult(connectionPool.Sftp.ReadAllText(path));
    }

    public Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        connectionPool.Ssh.CreateCommand($"cp {sourcePath} {destinationPath}").Execute();
        return Task.CompletedTask;
    }

    public string GetTemporaryFilename()
    {
        return JoinPaths("/tmp", Guid.NewGuid().ToString());
    }

    public bool FileOrDirectoryExists(string filename)
    {
        return connectionPool.Sftp.Exists(filename);
    }

    public void CreateTextFile(string filename)
    {
        connectionPool.Sftp.CreateText(filename);
    }

    public void CreateDirectory(string path)
    {
        var sftp = connectionPool.Sftp;
        var slices = path.Split('/').Skip(1).ToArray();
        for (var i = 0; i < slices.Length; ++i)
        {
            var directoryPath = '/' + JoinPaths(slices.Take(i + 1).ToArray());
            if (!sftp.Exists(directoryPath))
            {
                sftp.CreateDirectory(directoryPath);
            }
        }
    }

    public IEnumerable<string> GetSubdirectories(string path)
    {
        return connectionPool.Sftp
            .ListDirectory(path)
            .Where(x => x.IsDirectory && x.Name != "." && x.Name != "..")
            .Select(x => x.FullName);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return connectionPool.Sftp
            .ListDirectory(path)
            .Where(x => !x.IsDirectory)
            .Select(x => x.FullName);
    }

    public Task ExtractGzipAsync(string archivePath, string destinationPath)
    {
        var command = connectionPool.Ssh.CreateCommand($"tar -zxvf {archivePath} -C {destinationPath}");
        command.Execute();
        return Task.CompletedTask;
    }

    public void MakeFileExecutable(string path)
    {
        // TODO: investigate how this can be done better
        connectionPool.Sftp.ChangePermissions(path, mode: 777);
    }

    public void DeleteFile(string path)
    {
        connectionPool.Sftp.DeleteFile(path);
    }

    public void DeleteDirectoryRecursively(string path)
    {
        connectionPool.Ssh.CreateCommand($"rm -r {path}").Execute();
    }

    public string CreateTemporaryDirectory()
    {
        var temporaryDirectory = JoinPaths("/tmp", Guid.NewGuid().ToString());
        connectionPool.Sftp.CreateDirectory(temporaryDirectory);
        return temporaryDirectory;
    }

    public string JoinPaths(params string[] paths)
    {
        var finalBuilder = new StringBuilder();
        foreach (var path in paths)
        {
            finalBuilder.Append("/" + path);
        }

        return finalBuilder.ToString()
            .Replace("//", "/")
            .Replace("///", "/");
    }
}