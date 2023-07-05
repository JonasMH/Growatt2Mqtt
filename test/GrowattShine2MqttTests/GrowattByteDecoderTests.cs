using GrowattShine2Mqtt;
using NodaTime;

namespace GrowattShine2MqttTests;
public class GrowattByteDecoderTests
{
    private GrowattByteDecoder _sut = new();


    [Fact]
    public void TestString()
    {
        var input = "HelloWorld";

        var bytes = _sut.WriteString(input);

        var parsed = _sut.ReadString(bytes, 0, bytes.Length);

        Assert.Equal(input, parsed);
    }

    [Fact]
    public void TestShort()
    {
        var input = (short)-412;

        var bytes = _sut.WriteInt16(input);

        var parsed = _sut.ReadInt16(bytes, 0);

        Assert.Equal(input, parsed);
    }

    [Fact]
    public void TestUShort()
    {
        var input = (ushort)412;

        var bytes = _sut.WriteUInt16(input);

        var parsed = _sut.ReadUInt16(bytes, 0);

        Assert.Equal(input, parsed);
    }

    [Fact]
    public void TestInt()
    {
        var input = -3412;

        var bytes = _sut.WriteInt32(input);

        var parsed = _sut.ReadInt32(bytes, 0);

        Assert.Equal(input, parsed);
    }

    [Fact]
    public void TestUInt()
    {
        var input = (uint)3412;

        var bytes = _sut.WriteUInt32(input);

        var parsed = _sut.ReadUInt32(bytes, 0);

        Assert.Equal(input, parsed);
    }

    [Fact]
    public void TestDateTime()
    {
        var input = new LocalDateTime(2022, 05, 21, 21, 30);

        var bytes = _sut.WriteGrowattDateTime(input);

        var parsed = _sut.ReadGrowattDateTime(bytes, 0);

        Assert.Equal(input, parsed);
    }
}
