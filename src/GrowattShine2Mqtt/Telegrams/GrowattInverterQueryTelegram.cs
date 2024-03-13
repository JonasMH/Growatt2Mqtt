using System.Text;

namespace GrowattShine2Mqtt.Telegrams;

public class GrowattInverterQueryTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattInverterQueryTelegram()
    {
        Header = new GrowattTelegramHeader
        {
            MessageType = GrowattTelegramType.QUERY_INVERTER
        };
    }

    public GrowattTelegramHeader Header { get; set; }
    public string DataloggerId { get; set; }
    public ushort StartAddress { get; set; }
    public ushort EndAddress { get; set; }

    public byte[] ToBytes()
    {
        var buffer = new List<byte>();

        buffer.AddRange(Header.Bytes);
        buffer.AddRange(GrowattByteDecoder.Instance.WriteString(DataloggerId)); // 10 Bytes
        buffer.AddRange(new byte[20]); // Random space??
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16(StartAddress));
        buffer.AddRange(GrowattByteDecoder.Instance.WriteUInt16(EndAddress));

        return [.. buffer];
    }


}

public class GrowattInverterQueryResponseTelegram(GrowattTelegramHeader header) : IGrowattTelegram
{
    public GrowattTelegramHeader Header { get; set; } = header;

    public string DataloggerId { get; set; }
    public ushort Register { get; set; }
    public ushort Data { get; set; }

    public static GrowattInverterQueryResponseTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        var offset = 20;
        return new GrowattByteDecoderBuilder<GrowattInverterQueryResponseTelegram>(new GrowattInverterQueryResponseTelegram(header), bytes)
            .ReadString(x => x.DataloggerId, 8, 10)
            .ReadUInt16(x => x.Register, 18 + offset)
            .ReadUInt16(x => x.Data, 22 + offset)
            .Result;
    }

    public override string ToString()
    {
        return $"{GetType().Name}: Addr {Register} Data {Data.ToHex()}";
    }
}
