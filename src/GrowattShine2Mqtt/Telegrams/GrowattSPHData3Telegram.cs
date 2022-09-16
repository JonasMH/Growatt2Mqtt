using NodaTime;

namespace GrowattShine2Mqtt.Telegrams;

public class GrowattSPHData3Telegram : IGrowattTelegram
{
    public GrowattSPHData3Telegram(GrowattTelegramHeader header)
    {
        Header = header;
    }

    public GrowattTelegramHeader Header { get; set; }

    public string Datalogserial { get; set; }
    public string Pvserial { get; set; }
    public LocalDateTime? Date { get; set; }

    public static GrowattSPHData3Telegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        return new ByteDecoder<GrowattSPHData3Telegram>(new GrowattSPHData3Telegram(header), bytes)
            .ReadString(x => x.Datalogserial, 16 / 2, 10)
            .ReadString(x => x.Pvserial, 76 / 2, 10)
            .ReadGrowattDateTime(x => x.Date, 136 / 2)
            .Result;
    }
}

public class GrowattSPHData3TelegramAck : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattSPHData3TelegramAck(GrowattTelegramHeader header)
    {
        Header = header;
    }

    public GrowattTelegramHeader Header { get; set; }

    public byte[] ToBytes()
    {
        // In : 00 25 00 06 02 41 01 03 0d 22 2c 402040467734257761...
        // Out: 00 25 00 06 00 03 01 03 47 F7 D9
        //      ?? ?? ?? ?? ll ll tt tt data
        // ll ll = tt + data length
        // tt tt = message type

        var buffer = new List<byte>();

        buffer.AddRange(Header.Original[0..4]); // ??
        buffer.AddRange(new byte[] { 0x00, 0x00 }); // Make space for length
        buffer.AddRange(Header.Original[6..8]); // Add message type
        buffer.Add(0x00); // Add magic

        return buffer.ToArray();
    }
}
