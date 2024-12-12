using GrowattShine2Mqtt;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using ToMqttNet;

namespace GrowattShine2MqttTests;

public class MqttConnectionServiceMock : IMqttConnectionService
{
    public MqttConnectionOptions MqttOptions => new()
    {
        NodeId = "mynode"
    };
    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? OnApplicationMessageReceivedAsync;
    public event Func<MqttClientConnectedEventArgs, Task>? OnConnectAsync;
    public event Func<MqttClientDisconnectedEventArgs, Task>? OnDisconnectAsync;

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
    public GrowattTopicHelper _sut = new(new MqttConnectionServiceMock());

    [Fact]
    public void ShouldMatchBatteryFirst()
    {
        var result = _sut.TryGetDatalogger("mynode/write/jpc7a420fj/battery-first", out var dataLogger);

        Assert.True(result);
        Assert.Equal("jpc7a420fj", dataLogger);
    }
}
