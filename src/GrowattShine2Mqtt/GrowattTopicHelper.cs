using System.Text.Json;
using System.Text.RegularExpressions;
using ToMqttNet;

namespace GrowattShine2Mqtt;

public class GrowattTopicHelper(IMqttConnectionService mqttConnection)
{
    private readonly IMqttConnectionService _mqttConnection = mqttConnection;

    public string GetConnectedTopic()
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/connected";
    }

    public string GetDataPublishTopic(string dataLogger)
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/status/{dataLogger.ToLower()}/data";
    }

    public string GetInverterRegistryStatus(string dataLogger)
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/status/{dataLogger.ToLower()}/inverter-registers";
    }

    public string BatteryFirstModeTopic(string dataLogger)
    {
        return $"{_mqttConnection.MqttOptions.NodeId}/write/{dataLogger.ToLower()}/battery-first";
    }

    public bool TryParseBatteryFirstModeTopic(string topic, out string dataLogger)
    {
        var regex = new Regex(@$"{_mqttConnection.MqttOptions.NodeId}/write/([A-z0-9]+)/battery-first");
        var match = regex.Match(topic);

        if( !match.Success ) {
            dataLogger = "";
            return false;
        }

        dataLogger = match.Groups[0].Value;
        return true;
    }
}
