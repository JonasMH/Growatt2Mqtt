using GrowattShine2Mqtt;

namespace GrowattShine2MqttTests;

public class ByteArrayExtensionsTests
{
    [Fact]
    public void ShortToHex()
    {
        Assert.Equal("01F1", ((short)0x01F1).ToHex());
    }

    [Fact]
    public void UShortToHex()
    {
        Assert.Equal("01F1", ((ushort)0x01F1).ToHex());
    }

    [Fact]
    public void ByteArrayToHex()
    {
        Assert.Equal("4223", new byte[] {0x42, 0x23}.ToHex());
    }
}
