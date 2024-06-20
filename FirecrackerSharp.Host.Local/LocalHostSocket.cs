using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using FirecrackerSharp.Management;

namespace FirecrackerSharp.Host.Local;

internal sealed class LocalHostSocket(HttpClient httpClient) : IHostSocket
{
    public void Dispose()
    {
        httpClient.Dispose();
    }
    
    public async Task<ResponseWith<TReceived>> GetAsync<TReceived>(string uri, CancellationToken cancellationToken) where TReceived : class
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(uri, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ResponseWith<TReceived>.InternalError(VmManagement.Problems.CouldNotConnect);
        }
        catch (TaskCanceledException)
        {
            return ResponseWith<TReceived>.InternalError(VmManagement.Problems.TimedOut);
        }

        string responseJson;
        try
        {
            responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            return ResponseWith<TReceived>.InternalError(VmManagement.Problems.CouldNotReadResponseBody);
        }

        if (!response.IsSuccessStatusCode)
        {
            var (responseType, error) = ParseFault(response, responseJson);
            return responseType == ResponseType.BadRequest
                ? ResponseWith<TReceived>.BadRequest(error)
                : ResponseWith<TReceived>.InternalError(error);
        }

        TReceived obj;
        try
        {
            obj = JsonSerializer.Deserialize<TReceived>(responseJson, FirecrackerSerialization.Options)!;
        }
        catch (JsonException)
        {
            return ResponseWith<TReceived>.InternalError(VmManagement.Problems.CouldNotDeserializeJson);
        }

        return ResponseWith<TReceived>.Success(obj);
    }

    public Task<Response> PutAsync<TSent>(string uri, TSent content, CancellationToken cancellationToken) where TSent : class =>
        PutOrPatchAsync(content, c => httpClient.PutAsync(uri, c, cancellationToken));

    public Task<Response> PatchAsync<TSent>(string uri, TSent content, CancellationToken cancellationToken) where TSent : class =>
        PutOrPatchAsync(content, c => httpClient.PatchAsync(uri, c, cancellationToken));

    private static async Task<Response> PutOrPatchAsync<TSent>(TSent content,
        Func<StringContent, Task<HttpResponseMessage>> action)
    {
        var requestJson = JsonSerializer.Serialize(content, FirecrackerSerialization.Options);
        HttpResponseMessage response;
        try
        {
            var stringContent = new StringContent(requestJson, MediaTypeHeaderValue.Parse("application/json"));
            response = await action(stringContent);
        }
        catch (HttpRequestException)
        {
            return Response.InternalError(VmManagement.Problems.CouldNotConnect);
        }
        catch (TaskCanceledException)
        {
            return Response.InternalError(VmManagement.Problems.TimedOut);
        }
        
        if (response.IsSuccessStatusCode) return Response.Success;
        
        string responseJson;
        try
        {
            responseJson = await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return Response.InternalError(VmManagement.Problems.CouldNotReadResponseBody);
        }
        
        var (responseType, error) = ParseFault(response, responseJson);
        return responseType == ResponseType.BadRequest ? Response.BadRequest(error) : Response.InternalError(error);
    }
    
    private static (ResponseType, string) ParseFault(HttpResponseMessage response, string json)
    {
        var badRequest = response.StatusCode == HttpStatusCode.BadRequest;
        var faultMessage = VmManagement.Problems.CouldNotParseFaultMessage;
    
        try
        {
            faultMessage = JsonSerializer
                .Deserialize<JsonNode>(json, FirecrackerSerialization.Options)
                ?["fault_message"]
                ?.GetValue<string>() ?? VmManagement.Problems.CouldNotParseFaultMessage;
        }
        catch (JsonException) {}
    
        return badRequest ? (ResponseType.BadRequest, faultMessage) : (ResponseType.InternalError, faultMessage);
    }
}