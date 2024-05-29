using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FirecrackerSharp.Management;

namespace FirecrackerSharp.Host.Local;

public class LocalHostSocket(HttpClient httpClient) : IHostSocket
{
    public async Task<ManagementResponse> GetAsync<T>(string uri)
    {
        var response = await httpClient.GetAsync(uri);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return HandleFault(response, json);
        var obj = JsonSerializer.Deserialize<T>(json, FirecrackerSerialization.Options);
        return ManagementResponse.Ok(obj);
    }

    public async Task<ManagementResponse> PutAsync<T>(string uri, T content)
    {
        var requestJson = JsonSerializer.Serialize(content, FirecrackerSerialization.Options);
        var response = await httpClient.PutAsync(uri, new StringContent(requestJson));
        if (response.IsSuccessStatusCode) return ManagementResponse.NoContent;
        var responseJson = await response.Content.ReadAsStringAsync();
        return HandleFault(response, responseJson);
    }

    public async Task<ManagementResponse> PatchAsync<T>(string uri, T content)
    {
        var requestJson = JsonSerializer.Serialize(content, FirecrackerSerialization.Options);
        var response = await httpClient.PatchAsync(uri, new StringContent(requestJson));
        if (response.IsSuccessStatusCode) return ManagementResponse.NoContent;
        var responseJson = await response.Content.ReadAsStringAsync();
        return HandleFault(response, responseJson);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    private static ManagementResponse HandleFault(HttpResponseMessage response, string json)
    {
        var badRequest = response.StatusCode == HttpStatusCode.BadRequest;
        var faultMessage = "no fault message found";

        try
        {
            faultMessage = JsonSerializer
                .Deserialize<JsonNode>(json, FirecrackerSerialization.Options)
                ?["fault_message"]
                ?.GetValue<string>() ?? "no fault message found";
        }
        catch (JsonException) {}

        return badRequest ? ManagementResponse.BadRequest(faultMessage) : ManagementResponse.InternalError(faultMessage);
    }
}