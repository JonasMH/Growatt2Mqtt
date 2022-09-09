using GrowattShine2Mqtt;
using GrowattShine2Mqtt.Telegrams;

namespace GrowattShine2MqttTests;

public class GrowattTelegramHeaderTests
{
    [Fact]
    public void Test1()
    {
        var input = "00300006033f0104".ParseHex();

        var result = GrowattTelegramHeader.Parse(input);

        Assert.Equal(831, result.MessageLength);
        Assert.Equal(GrowattTelegramType.DATA4, result.MessageType);
    }
}
