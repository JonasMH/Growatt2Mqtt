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
        return new GrowattByteDecoderBuilder<GrowattSPHData3Telegram>(new GrowattSPHData3Telegram(header), bytes)
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
        var buffer = new List<byte>();

        buffer.AddRange(Header.Bytes[0..4]); // ??
        buffer.AddRange(new byte[] { 0x00, 0x00 }); // Make space for length
        buffer.AddRange(Header.Bytes[6..8]); // Add message type
        buffer.Add(0x00); // Add magic

        return buffer.ToArray();
    }
}
