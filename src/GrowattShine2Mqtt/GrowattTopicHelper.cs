using ToMqttNet;

namespace GrowattShine2Mqtt;

public interface IGrowattTopicHelper
{
    string GetConnectedTopic();

    string GetDataPublishTopic(string dataLogger);
}

public class GrowattTopicHelper : IGrowattTopicHelper
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
