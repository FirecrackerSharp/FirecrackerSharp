using System.Text.Json;
using System.Text.Json.Nodes;
using FirecrackerSharp.Management;

namespace FirecrackerSharp.Host.Ssh;

internal sealed class SshHostSocket(
    ConnectionPool connectionPool,
    CurlConfiguration curlConfiguration,
    string baseUri,
    string socketPath) : IHostSocket
{
    private const string CurlHttpCode = "%{http_code}";

    private readonly string _retryArgs = curlConfiguration.RetryAmount <= 0
        ? string.Empty
        : $"--retry {curlConfiguration.RetryAmount} --retry-delay {curlConfiguration.RetryDelaySeconds}" +
          $" --retry-max-time {curlConfiguration.RetryMaxTimeSeconds}";

    public void Dispose() {}

    public Task<ResponseWith<TReceived>> GetAsync<TReceived>(string uri, CancellationToken cancellationToken)
        where TReceived : class => MakeReceiveRequestAsync<TReceived>(uri, cancellationToken);

    public Task<Response> PutAsync<TSent>(string uri, TSent content, CancellationToken cancellationToken) where TSent : class =>
        MakeSendRequestAsync("PUT", uri, content, cancellationToken);

    public Task<Response> PatchAsync<TSent>(string uri, TSent content, CancellationToken cancellationToken) where TSent : class =>
        MakeSendRequestAsync("PATCH", uri, content, cancellationToken);

    private async Task<Response> MakeSendRequestAsync<TSent>(string method, string uri, TSent content, CancellationToken cancellationToken)
    {
        try
        {
            var requestBody = JsonSerializer.Serialize(content, FirecrackerSerialization.Options);
            var (responseType, error, _) = await MakeRequestAsync(method, uri, requestBody, cancellationToken);
            if (responseType == ResponseType.Success) return Response.Success;

            return responseType == ResponseType.BadRequest
                ? Response.BadRequest(error!)
                : Response.InternalError(error!);
        }
        catch (TaskCanceledException)
        {
            return Response.InternalError(VmManagement.Problems.TimedOut);
        }
    }

    private async Task<ResponseWith<TReceived>> MakeReceiveRequestAsync<TReceived>(
        string uri, CancellationToken cancellationToken) where TReceived : class
    {
        try
        {
            var (responseType, error, responseBody) = await MakeRequestAsync(method: "GET", uri, body: null, cancellationToken);
            
            if (responseType == ResponseType.Success)
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<TReceived>(responseBody!, FirecrackerSerialization.Options);
                    return ResponseWith<TReceived>.Success(obj!);
                }
                catch (Exception)
                {
                    return ResponseWith<TReceived>.InternalError(VmManagement.Problems.CouldNotDeserializeJson);
                }
            }

            return responseType == ResponseType.BadRequest
                ? ResponseWith<TReceived>.BadRequest(error!)
                : ResponseWith<TReceived>.InternalError(error!);
        }
        catch (TaskCanceledException)
        {
            return ResponseWith<TReceived>.InternalError(VmManagement.Problems.TimedOut);
        }
    }
    
    private async Task<(ResponseType, string?, string?)> MakeRequestAsync(string method, string uri, string? body,
        CancellationToken cancellationToken)
    {
        var ssh = connectionPool.Ssh;
        var sftp = connectionPool.Sftp;

        var responseFile = $"/tmp/{Guid.NewGuid()}";

        uri = uri.TrimStart('/');
        var fullUri = baseUri + '/' + uri;
        var bodyArg = body is null ? string.Empty : $"-d '{body}' -H \"Content-Type: application/json\"";
        var command = ssh.CreateCommand(
            $"curl -X {method} --unix-socket {socketPath} -s -w {CurlHttpCode} -o {responseFile} {_retryArgs} {bodyArg} {fullUri}");
        var asyncResult = command.BeginExecute();
        while (true)
        {
            await Task.Delay(curlConfiguration.PollFrequency, cancellationToken);
            if (asyncResult.IsCompleted) break;
        }

        var result = command.EndExecute(asyncResult);

        if (result is null || !int.TryParse(result, out var responseCode))
        {
            return (ResponseType.InternalError, VmManagement.Problems.CouldNotReceiveStatusCode, null);
        }
        
        var responseText = sftp.ReadAllText(responseFile);
        await sftp.DeleteFileAsync(responseFile, cancellationToken);
        if (responseText is null)
        {
            return (ResponseType.InternalError, VmManagement.Problems.CouldNotReadResponseBody, null);
        }
        
        if (responseCode < 400) return (ResponseType.Success, null, responseText);

        var faultMessage = VmManagement.Problems.CouldNotParseFaultMessage;
        try
        {
            faultMessage = JsonSerializer.Deserialize<JsonNode>(responseText, FirecrackerSerialization.Options)
                ?["fault_message"]
                ?.GetValue<string>() ?? VmManagement.Problems.CouldNotParseFaultMessage;
        }
        catch (JsonException)
        {
            // ignored
        }

        return responseCode < 500
            ? (ResponseType.BadRequest, $"received {responseCode}: {faultMessage}", null)
            : (ResponseType.InternalError, $"received {responseCode}: {faultMessage}", null);
    }
}