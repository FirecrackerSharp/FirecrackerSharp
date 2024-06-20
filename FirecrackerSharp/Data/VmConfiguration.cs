using System.Text.Json.Serialization;
using FirecrackerSharp.Core;
using FirecrackerSharp.Data.Ballooning;
using FirecrackerSharp.Data.Drives;
using FirecrackerSharp.Data.Observability;
using FirecrackerSharp.Tty;

namespace FirecrackerSharp.Data;

public sealed record VmConfiguration(
    [property: JsonPropertyName("boot-source")]
    VmBootSource BootSource,
    [property: JsonPropertyName("machine-config")]
    VmMachineConfiguration MachineConfiguration,
    [property: JsonPropertyName("drives")] IEnumerable<VmDrive> Drives,
    [property: JsonPropertyName("balloon")]
    VmBalloon? Balloon = null,
    [property: JsonPropertyName("logger")] VmLogger? Logger = null,
    [property: JsonPropertyName("metrics")]
    VmMetrics? Metrics = null,
    [property: JsonPropertyName("entropy")]
    VmEntropyDevice? EntropyDevice = null,
    [property: JsonPropertyName("network-interfaces")]
    IEnumerable<VmNetworkInterface>? NetworkInterfaces = null,
    [property: JsonPropertyName("vsock")] VmVsock? Vsock = null,
    [property: JsonIgnore] TtyAuthentication? TtyAuthentication = null,
    [property: JsonIgnore]
    VmConfigurationApplicationMode ApplicationMode = VmConfigurationApplicationMode.JsonConfiguration)
{
    [JsonIgnore]
    public WaitOptions WaitOptions { get; init; } = WaitOptions.Default;
}