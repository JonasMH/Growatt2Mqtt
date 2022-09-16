namespace GrowattShine2Mqtt.Telegrams;

public enum GrowattTelegramType
{
    DATA3 = 0x0103,
    DATA4 = 0x0104,
    QUERY_INVERTER = 0x0105,
    COMMAND_INVERTER = 0x0106,
    PING = 0x0116,
    COMMAND_DATALOGGER = 0x0118,
    QUERY_DATALOGGER = 0x0119
}
