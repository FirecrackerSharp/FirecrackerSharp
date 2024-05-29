using System.Text.Json;
using System.Text.Json.Serialization;

namespace FirecrackerSharp;

/// <summary>
/// A static class containing all options pertaining to serialization of Firecracker-internal data.
///
/// This shouldn't be used by the SDK users, as it is public in order to allow for host implementations to use shared
/// serialization information.
/// </summary>
public static class FirecrackerSerialization
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used for and by Firecracker (ignore nulls, snake case, enums as strings)
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}