using System.Text.Json;
using System.Text.Json.Nodes;
using FirecrackerSharp.Management;

namespace FirecrackerSharp.Host.Ssh;

internal class SshHostSocket(
    ConnectionPool connectionPool,
    CurlConfiguration curlConfiguration,
    string baseAddress,
    string socketAddress) : IHostSocket
{
    public Task<ManagementResponse> GetAsync<T>(string uri)
    {
        return Task.FromResult(ExecuteContentCommand<T>(method: "GET", uri, args: string.Empty));
    }

    public Task<ManagementResponse> PutAsync<T>(string uri, T content)
    {
        return Task.FromResult(ExecuteNoContentCommand(method: "PUT", uri, content));
    }

    public Task<ManagementResponse> PatchAsync<T>(string uri, T content)
    {
        return Task.FromResult(ExecuteNoContentCommand(method: "PATCH", uri, content));
    }

    public void Dispose() {}

    private ManagementResponse ExecuteNoContentCommand<T>(string method, string uri, T body)
    {
        var bodyJson = JsonSerializer.Serialize(body, FirecrackerSerialization.Options);
        return ExecuteCommand(method, uri, $"-H \"Content-Type: application/json\" -d '{bodyJson}'",
            _ => ManagementResponse.NoContent);
    }

    private ManagementResponse ExecuteContentCommand<T>(string method, string uri, string args)
        => ExecuteCommand(method, uri, args,
            json =>
            { 
                var obj = JsonSerializer.Deserialize<T>(json, FirecrackerSerialization.Options);
                return ManagementResponse.Ok(obj);
            });

    private ManagementResponse ExecuteCommand(string method, string uri, string args,
        Func<string, ManagementResponse> okHandler)
    {
        var ssh = connectionPool.Ssh;
        var sftp = connectionPool.Sftp;

        var responseFile = $"/tmp/{Guid.NewGuid()}";
        
        const string verbatim = "%{http_code}";
        var actualUri = (baseAddress + "/" + uri).Replace("//", "/");
        var command = ssh.CreateCommand($"curl -X {method} --unix-socket {socketAddress} -o {responseFile} -s -w {verbatim} {args} {actualUri}");
        command.CommandTimeout = curlConfiguration.Timeout;
        var result = command.Execute();
        
        var responseText = sftp.ReadAllText(responseFile);
        sftp.DeleteFile(responseFile);
        if (responseText is null)
            return ManagementResponse.InternalError("could not read response file");
        
        if (result is null || !int.TryParse(result, out var responseCode))
            return ManagementResponse.InternalError("could not receive HTTP status code");

        if (responseCode < 400) return okHandler(responseText);

        var faultMessage = "no fault message found";
        try
        {
            faultMessage = JsonSerializer.Deserialize<JsonNode>(responseText, FirecrackerSerialization.Options)
                ?["fault_message"]
                ?.GetValue<string>() ?? "no fault message found";
        }
        catch (JsonException) {}

        return responseCode < 500
            ? ManagementResponse.BadRequest(faultMessage)
            : ManagementResponse.InternalError(faultMessage);
    }
}