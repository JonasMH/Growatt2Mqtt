using System.Text.Json;
using System.Text.Json.Serialization;
using ToMqttNet;

namespace GrowattShine2Mqtt;

[JsonSerializable(typeof(GrowattStatusPayload))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
//    ,Converters = new[] { typeof(NodaTimeInstantConverter) }
)]
public partial class GrowattMqttJsonSerializerContext : JsonSerializerContext
{

}

public class GrowattTopicHelper
{
    private readonly IMqttConnectionService _mqttConnection;

    public GrowattTopicHelper(IMqttConnectionService mqttConnection)
    {
        _mqttConnection = mqttConnection;
    }

    public string GetConnectedTopic()
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/connected";
    }

    public string GetDataPublishTopic(string dataLogger)
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/status/{dataLogger.ToLower()}";
    }
}
