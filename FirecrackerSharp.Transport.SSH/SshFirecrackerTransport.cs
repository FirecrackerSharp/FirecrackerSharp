using System.Text;
using Renci.SshNet;

namespace FirecrackerSharp.Transport.SSH;

public class SshFirecrackerTransport(ConnectionInfo connectionInfo) : IFirecrackerTransport
{
    private SshClient Ssh
    {
        get
        {
            var client = new SshClient(connectionInfo);
            client.Connect();
            return client;
        }
    }

    private SftpClient Sftp
    {
        get
        {
            var client = new SftpClient(connectionInfo);
            client.Connect();
            return client;
        }
    }
    
    public Task WriteTextFileAsync(string path, string content)
    {
        using var sftp = Sftp;
        sftp.WriteAllText(path, content);
        return Task.CompletedTask;
    }

    public Task WriteBinaryFileAsync(string path, byte[] content)
    {
        using var sftp = Sftp;
        sftp.WriteAllBytes(path, content);
        return Task.CompletedTask;
    }

    public Task<string> ReadTextFileAsync(string path)
    {
        using var sftp = Sftp;
        return Task.FromResult(sftp.ReadAllText(path));
    }

    public Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        using var ssh = Ssh;
        ssh.CreateCommand($"cp {sourcePath} {destinationPath}").Execute();
        return Task.CompletedTask;
    }

    public string GetTemporaryFilename()
    {
        return JoinPaths("/tmp", Guid.NewGuid().ToString());
    }

    public void CreateDirectory(string path)
    {
        using var sftp = Sftp;
        var slices = path.Split('/').Skip(1).ToArray();
        for (var i = 0; i < slices.Length; ++i)
        {
            var directoryPath = '/' + Path.Join(slices.Take(i + 1).ToArray());
            if (!sftp.Exists(directoryPath))
            {
                sftp.CreateDirectory(directoryPath);
            }
        }
    }

    public IEnumerable<string> GetSubdirectories(string path)
    {
        using var sftp = Sftp;
        return sftp
            .ListDirectory(path)
            .Where(x => x.IsDirectory && x.Name != "." && x.Name != "..")
            .Select(x => x.FullName);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        using var sftp = Sftp;
        return sftp
            .ListDirectory(path)
            .Where(x => !x.IsDirectory)
            .Select(x => x.FullName);
    }

    public Task ExtractGzipAsync(string archivePath, string destinationPath)
    {
        using var ssh = Ssh;
        var command = ssh.CreateCommand($"tar -zxvf {archivePath} -C {destinationPath}");
        command.Execute();
        return Task.CompletedTask;
    }

    public void MakeFileExecutable(string path)
    {
        // TODO: investigate how this can be done better
        using var sftp = Sftp;
        sftp.ChangePermissions(path, mode: 777);
    }

    public void DeleteFile(string path)
    {
        using var sftp = Sftp;
        sftp.DeleteFile(path);
    }

    public void DeleteDirectoryRecursively(string path)
    {
        using var ssh = Ssh;
        ssh.CreateCommand($"rm -r {path}").Execute();
    }

    public string CreateTemporaryDirectory()
    {
        using var sftp = Sftp;
        
        var temporaryDirectory = JoinPaths("/tmp", Guid.NewGuid().ToString());
        sftp.CreateDirectory(temporaryDirectory);
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

    public IFirecrackerProcess LaunchProcess(string executable, string args)
    {
        var ssh = Ssh;
        var command = ssh.CreateCommand(executable + " " + args);
        return new SshFirecrackerProcess(command, ssh);
    }
}