namespace GrowattShine2Mqtt.Telegrams;

public interface IGrowattTelegram
{
    GrowattTelegramHeader Header { get; }

}


public interface ISerializeableGrowattTelegram : IGrowattTelegram
{
    byte[] ToBytes();
}

