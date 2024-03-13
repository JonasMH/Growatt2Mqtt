using System.Text.Json.Serialization;

namespace GrowattShine2Mqtt;

[JsonSerializable(typeof(GrowattStatusPayload))]
[JsonSerializable(typeof(Dictionary<ushort, ushort>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
//    ,Converters = new[] { typeof(NodaTimeInstantConverter) }
)]
public partial class GrowattMqttJsonSerializerContext : JsonSerializerContext
{

}
