using System.Text.Json.Serialization;

namespace FirecrackerSharp.Data;

public record FirecrackerVmConfiguration(
    [property: JsonPropertyName("boot-source")] VmBootSource BootSource,
    [property: JsonPropertyName("machine-config")] VmMachineConfiguration MachineConfiguration,
    [property: JsonPropertyName("drives")] IEnumerable<VmDrive> Drives);