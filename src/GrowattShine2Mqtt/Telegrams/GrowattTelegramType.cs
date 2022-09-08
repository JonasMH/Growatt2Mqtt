namespace GrowattShine2Mqtt.Telegrams;

public enum GrowattTelegramType
{
    DATA3 = 0x0103,
    DATA4 = 0x0104,
    PING = 0x0116,
    CONFIGURE = 0x0118,
    IDENTITY = 0x0119
}
