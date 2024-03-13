using System.Text;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Win32;

namespace GrowattShine2Mqtt.Telegrams;

public class GrowattDataloggerQueryTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattDataloggerQueryTelegram()
    {
        Header = new GrowattTelegramHeader
        {
            MessageType = GrowattTelegramType.QUERY_DATALOGGER
        };
    }

    public GrowattTelegramHeader Header { get; set; }
    public string LoggerId { get; set; }
    public ushort StartAddress { get; set; }
    public ushort EndAddress { get; set; }

    public byte[] ToBytes()
    {
        var buffer = new List<byte>();

        buffer.AddRange(Header.Bytes);
        buffer.AddRange(GrowattByteDecoder.Instance.WriteString(LoggerId)); // 10 Bytes
        buffer.AddRange(new byte[20]); // Random space??
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16(StartAddress));
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16(EndAddress));

        return [.. buffer];
    }
}

public class GrowattDataloggerQueryResponseTelegram(GrowattTelegramHeader header) : IGrowattTelegram
{
    public GrowattTelegramHeader Header { get; set; } = header;

    public string Datalogserial { get; set; }
    public ushort Register { get; set; }
    public short DataLength { get; set; }
    public byte[] Data { get; set; }

    public static GrowattDataloggerQueryResponseTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        var offset = 20;
        var result = new GrowattByteDecoderBuilder<GrowattDataloggerQueryResponseTelegram>(new GrowattDataloggerQueryResponseTelegram(header), bytes)
            .ReadString(x => x.Datalogserial, 8, 10)
            .ReadUInt16(x => x.Register, 18 + offset)
            .ReadInt16(x => x.DataLength, 20 + offset)
            .Result;

        result.Data = bytes.Skip(22 + offset).Take(result.DataLength).ToArray();

        return result;
    }

    public override string ToString()
    {
        return $"{GetType().Name}: Addr {Register} Len {DataLength} Data {Data.ToHex()}";
    }
}
