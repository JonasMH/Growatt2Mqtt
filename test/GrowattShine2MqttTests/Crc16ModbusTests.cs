using GrowattShine2Mqtt;
using GrowattShine2Mqtt.Telegrams;

namespace GrowattShine2MqttTests;

public class Crc16ModbusTests
{
    private readonly Crc16Modbus _sut;

    public Crc16ModbusTests()
    {
        _sut = new Crc16Modbus();
    }

    [Fact]
    public void Test1()
    {
        var result = _sut.ComputeChecksum("000200060003010347".ParseHex());

        Assert.Equal(0x099A, result);
    }
}
