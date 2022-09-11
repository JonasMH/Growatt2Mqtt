using System.Text.Json;
using ToMqttNet;

namespace GrowattShine2Mqtt;

public interface IGrowattTopicHelper
{
    string GetConnectedTopic();

    string GetDataPublishTopic(string dataLogger);

    string GetPayloadPropertyName(string csharpPropertyName);
    string SerializePayload<T>(T payload);
}

public class GrowattTopicHelper : IGrowattTopicHelper
{
    private readonly IMqttConnectionService _mqttConnection;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GrowattTopicHelper(IMqttConnectionService mqttConnection)
    {
        _mqttConnection = mqttConnection;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public string GetConnectedTopic()
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/connected";
    }

    public string GetDataPublishTopic(string dataLogger)
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/status/{dataLogger.ToLower()}";
    }

    public string SerializePayload<T>(T payload)
    {
        return JsonSerializer.Serialize(payload, _jsonSerializerOptions);
    }

    public string GetPayloadPropertyName(string csharpPropertyName)
    {
        return _jsonSerializerOptions.PropertyNamingPolicy!.ConvertName(csharpPropertyName);
    }
}
