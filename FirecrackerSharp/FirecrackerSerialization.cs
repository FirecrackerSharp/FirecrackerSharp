using System.Text.Json;
using System.Text.Json.Serialization;

namespace FirecrackerSharp;

public static class FirecrackerSerialization
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}