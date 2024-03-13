namespace GrowattShine2Mqtt.Telegrams;

public class GrowattSPHPingTelegram(GrowattTelegramHeader header) : IGrowattTelegram
{
    public GrowattTelegramHeader Header { get; set; } = header;

    public string DataLoggerId { get; set; }

    public static GrowattSPHPingTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        return new GrowattByteDecoderBuilder<GrowattSPHPingTelegram>(new GrowattSPHPingTelegram(header), bytes)
            .ReadString(x => x.DataLoggerId, 8, 10)
            .Result;
    }
}
