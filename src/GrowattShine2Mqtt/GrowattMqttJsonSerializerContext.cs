using System.Text.Json.Serialization;

namespace GrowattShine2Mqtt;

[JsonSerializable(typeof(GrowattStatusPayload))]
[JsonSerializable(typeof(GrowattRegistersStatus))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
//    ,Converters = new[] { typeof(NodaTimeInstantConverter) }
)]
public partial class GrowattMqttJsonSerializerContext : JsonSerializerContext
{

}


public record GrowattRegistersStatus
{
    public Dictionary<ushort, GrowattRegisterStatus> Registers { get; init; } = new();
}

public record GrowattRegisterStatus
{
    public ushort RawValue { get; init; }
    public string Name { get; init; } = string.Empty;
}