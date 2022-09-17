using System.Text;

namespace GrowattShine2Mqtt.Telegrams;

public class GrowattInverterQueryTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattInverterQueryTelegram()
    {
        Header = new GrowattTelegramHeader()
        {
            Original = new byte[8]
            {
                0x00, 0x01, 0x00, 0x06, 0x00, 0x00, 0x01, 0x05
            }
        };
    }

    public GrowattTelegramHeader Header { get; set; }
    public string LoggerId { get; set; }
    public ushort StartingAddress { get; set; }
    public ushort EndAddress { get; set; }

    public byte[] ToBytes()
    {
        // In : 00 01 00 06 00 37 01 18 0d 22 ..
        //      ?? ?? ?? ?? ll ll tt tt data
        // ll ll = tt + data length
        // tt tt = message type

        var buffer = new List<byte>();

        buffer.AddRange(Header.Original[0..4]); // ??
        buffer.AddRange(new byte[2]); // Make space for length
        buffer.AddRange(Header.Original[6..8]); // Add message type
        buffer.AddRange(Encoding.UTF8.GetBytes(LoggerId)); // 10 Bytes
        buffer.AddRange(new byte[20]); // Random space??
        buffer.AddRange(BitConverter.GetBytes(StartingAddress).Reverse());
        buffer.AddRange(BitConverter.GetBytes(EndAddress).Reverse());

        return buffer.ToArray();
    }


}

public class GrowattInverterQueryResponseTelegram : IGrowattTelegram
{
    public GrowattInverterQueryResponseTelegram(GrowattTelegramHeader header)
    {
        Header = header;
    }

    public GrowattTelegramHeader Header { get; set; }

    public string Datalogserial { get; set; }
    public ushort Register { get; set; }
    public ushort Data { get; set; }

    public static GrowattInverterQueryResponseTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        // In : 00 01 00 06 00 37 01 18 0d 22 ..
        //      ?? ?? ?? ?? ll ll tt tt rr rr
        // ll ll = tt + data length
        // tt tt = message type
        // rr rr = register

        var offset = 20;
        return new ByteDecoder<GrowattInverterQueryResponseTelegram>(new GrowattInverterQueryResponseTelegram(header), bytes)
            .ReadString(x => x.Datalogserial, 8, 10)
            .ReadUInt16(x => x.Register, 18 + offset)
            .ReadUInt16(x => x.Data, 22 + offset)
            .Result;
    }

    public override string ToString()
    {
        return $"{GetType().Name}: Addr {Register} Data {Data.ToHex()}";
    }
}
