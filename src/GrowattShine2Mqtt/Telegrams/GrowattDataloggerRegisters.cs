namespace GrowattShine2Mqtt.Telegrams;

public enum GrowattDataloggerRegisters
{
    DATALOGGER_SERIAL = 0x0800,
    DATALOGGER_MAC_ADDRESS = 0x1000,
    DATALOGGER_GATEWAY_IP = 0x0E00,
    SERVER_ADDRESS = 0x0013,
    GROWATT_ADDRESS = 0x1100,
    GROWATT_SERVER_PORT = 0x1200,
    TIME = 0x001F,
    REBOOT = 0x0020
}
