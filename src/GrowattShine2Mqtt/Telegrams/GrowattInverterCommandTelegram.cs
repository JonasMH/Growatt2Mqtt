using System.Text;

namespace GrowattShine2Mqtt.Telegrams;

public class GrowattInverterCommandTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattInverterCommandTelegram()
    {
        Header = new GrowattTelegramHeader
        {
            MessageType = GrowattTelegramType.COMMAND_INVERTER
        };
    }

    public GrowattInverterCommandTelegram(GrowattTelegramHeader header)
    {
        Header = header;
    }

    public GrowattTelegramHeader Header { get; set; }

    public string DataloggerId { get; set; }
    public ushort Register { get; set; }
    public ushort Value { get; set; }


    public byte[] ToBytes()
    {
        var buffer = new List<byte>();

        buffer.AddRange(Header.Bytes);
        buffer.AddRange(GrowattByteDecoder.Instance.WriteString(DataloggerId)); // 10 Bytes
        buffer.AddRange(new byte[20]); // Random space??
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16(Register));
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16(Value));

        return [.. buffer];
    }


    public static GrowattInverterCommandTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        var offset = 20;
        return new GrowattByteDecoderBuilder<GrowattInverterCommandTelegram>(new GrowattInverterCommandTelegram(header), bytes)
            .ReadString(x => x.DataloggerId, 8, 10)
            .ReadUInt16(x => x.Register, 18 + offset)
            .ReadUInt16(x => x.Value, 20 + offset)
            .Result;
    }
}

public class GrowattInverterCommandResponseTelegram(GrowattTelegramHeader header) : IGrowattTelegram
{
    public GrowattTelegramHeader Header { get; set; } = header;

    public string DataloggerId { get; set; }
    public byte Result { get; set; }
    public ushort Register { get; set; }
    public ushort Data { get; set; }

    public static GrowattInverterCommandResponseTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        var offset = 20;
        return new GrowattByteDecoderBuilder<GrowattInverterCommandResponseTelegram>(new GrowattInverterCommandResponseTelegram(header), bytes)
            .ReadString(x => x.DataloggerId, 8, 10)
            .ReadUInt16(x => x.Register, 18 + offset)
            .ReadByte(x => x.Result, 20 + offset)
            .ReadUInt16(x => x.Data, 21 + offset)
            .Result;
    }
}


// For COMMAND_INVERTER
// <sendseq> 00 <protocol> <length> 01 <command> <dataloggerid> "0000000000000000000000000000000000000000" <register 2 bytes> <value> <crc 2 bytes>
