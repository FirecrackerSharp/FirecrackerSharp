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
        return ExecuteContentCommand<T>(method: "GET", uri, args: string.Empty);
    }

    public Task<ManagementResponse> PutAsync<T>(string uri, T content)
    {
        return ExecuteNoContentCommand(method: "PUT", uri, content);
    }

    public Task<ManagementResponse> PatchAsync<T>(string uri, T content)
    {
        return ExecuteNoContentCommand(method: "PATCH", uri, content);
    }

    public void Dispose() {}

    private Task<ManagementResponse> ExecuteNoContentCommand<T>(string method, string uri, T body)
    {
        var bodyJson = JsonSerializer.Serialize(body, FirecrackerSerialization.Options);
        return ExecuteCommand(method, uri, $"-H \"Content-Type: application/json\" -d '{bodyJson}'",
            _ => ManagementResponse.NoContent);
    }

    private Task<ManagementResponse> ExecuteContentCommand<T>(string method, string uri, string args)
        => ExecuteCommand(method, uri, args,
            json =>
            { 
                var obj = JsonSerializer.Deserialize<T>(json, FirecrackerSerialization.Options);
                return ManagementResponse.Ok(obj);
            });

    private async Task<ManagementResponse> ExecuteCommand(string method, string uri, string args,
        Func<string, ManagementResponse> okHandler)
    {
        var ssh = connectionPool.Ssh;
        var sftp = connectionPool.Sftp;

        var responseFile = $"/tmp/{Guid.NewGuid()}";
        sftp.CreateText(responseFile).Close();
        
        const string verbatim = "%{http_code}";
        var command = ssh.CreateCommand($"curl -X {method} --unix-socket {socketAddress} -o {responseFile} -s -w {verbatim} {args} {baseAddress}/{uri}");
        command.CommandTimeout = curlConfiguration.Timeout;
        var asyncResult = command.BeginExecute();
        while (!asyncResult.IsCompleted)
        {
            await Task.Delay(curlConfiguration.PollFrequency);
        }
        command.EndExecute(asyncResult);
        
        var responseText = sftp.ReadAllText(responseFile);
        sftp.DeleteFile(responseFile);
        if (responseText is null)
            return ManagementResponse.InternalError("could not read response file");
        
        if (!int.TryParse(command.Result, out var responseCode))
            return ManagementResponse.InternalError("could not receive HTTP status code");

        if (responseCode < 400) return okHandler(responseText);

        var faultMessage = JsonSerializer.Deserialize<JsonNode>(responseText, FirecrackerSerialization.Options)
            !["fault_message"]
            !.GetValue<string>();

        return responseCode < 500
            ? ManagementResponse.BadRequest(faultMessage)
            : ManagementResponse.InternalError(faultMessage);
    }
}