using System.Globalization;
using System.Text;
using NodaTime;

namespace GrowattShine2Mqtt.Telegrams;


public class GrowattDataloggerCommandTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattDataloggerCommandTelegram()
    {
        Header = new GrowattTelegramHeader
        {
            MessageType = GrowattTelegramType.COMMAND_DATALOGGER
        };
    }

    public GrowattTelegramHeader Header { get; set; }

    public string LoggerId { get; set; } = "";
    public ushort Register { get; set; }
    public byte[] Value { get; set; } = [];


    public byte[] ToBytes()
    {
        var buffer = new List<byte>();

        buffer.AddRange(Header.Bytes);
        buffer.AddRange(GrowattByteDecoder.Instance.WriteString(LoggerId)); // 10 Bytes
        buffer.AddRange(new byte[20]); // Random space??
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16(Register));
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16((ushort)Value.Length));
        buffer.AddRange(Value);

        return [.. buffer];
    }

    public static GrowattDataloggerCommandTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        return new GrowattByteDecoderBuilder<GrowattDataloggerCommandTelegram>(new GrowattDataloggerCommandTelegram() { Header = header }, bytes)
            .Result;
    }
}

public static class GrowattDataloggerCommandTelegramExtensions
{
    public static GrowattDataloggerCommandTelegram CreateTimeCommand(this GrowattDataloggerCommandTelegram telegram, LocalDateTime dateTime)
    {
        telegram.Register = (ushort)GrowattDataloggerRegisters.TIME;
        telegram.Value = Encoding.UTF8.GetBytes(dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        return telegram;
    }
}
