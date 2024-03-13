using GrowattShine2Mqtt;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using ToMqttNet;

namespace GrowattShine2MqttTests;

public class MqttConnectionServiceMock : IMqttConnectionService
{
    public MqttConnectionOptions MqttOptions => new MqttConnectionOptions
    {
        NodeId = "mynode"
    };

    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? OnApplicationMessageReceived;
    public event EventHandler<EventArgs>? OnConnect;
    public event EventHandler<EventArgs>? OnDisconnect;

    public Task PublishAsync(MqttApplicationMessage applicationMessages)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeAsync(params MqttTopicFilter[] topics)
    {
        throw new NotImplementedException();
    }

    public Task UnsubscribeAsync(params string[] topics)
    {
        throw new NotImplementedException();
    }
}

public class TopicHelperTests
{
    public GrowattTopicHelper _sut = new GrowattTopicHelper(new MqttConnectionServiceMock());

    [Fact]
    public void ShouldMatchBatteryFirst()
    {
        var result = _sut.TryParseBatteryFirstModeTopic("mynode/write/jpc7a420fj/battery-first", out var dataLogger);

        Assert.True(result);
        Assert.Equal("jpc7a420fj", dataLogger);
    }
}
