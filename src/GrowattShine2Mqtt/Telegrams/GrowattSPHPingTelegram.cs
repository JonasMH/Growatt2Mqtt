namespace GrowattShine2Mqtt.Telegrams;

public class GrowattSPHPingTelegram : IGrowattTelegram
{
    public GrowattSPHPingTelegram(GrowattTelegramHeader header)
    {
        Header = header;
    }

    public GrowattTelegramHeader Header { get; set; }

    public string DataLoggerId { get; set; }

    public static GrowattSPHPingTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        return new GrowattByteDecoderBuilder<GrowattSPHPingTelegram>(new GrowattSPHPingTelegram(header), bytes)
            .ReadString(x => x.DataLoggerId, 8, 10)
            .Result;
    }
}
