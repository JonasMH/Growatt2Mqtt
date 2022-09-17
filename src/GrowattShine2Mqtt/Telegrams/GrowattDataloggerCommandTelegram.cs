using System.Globalization;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using NodaTime;

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


public class GrowattDataloggerCommandTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattDataloggerCommandTelegram()
    {
        Header = new GrowattTelegramHeader()
        {
            Original = new byte[8]
            {
                0x00, 0x01, 0x00, 0x06, 0x00, 0x00, 0x01, 0x18
            }
        };
    }

    public GrowattTelegramHeader Header { get; set; }

    public string LoggerId { get; set; }
    public ushort Register { get; set; }
    public byte[] Value { get; set; }


    public byte[] ToBytes()
    {
        // In : 00 01 00 06 00 37 01 18 0d 22 ..
        //      ?? ?? ?? pp ll ll tt tt data
        // ll ll = tt + data length
        // tt tt = message type
        // pp = protocol version

        var buffer = new List<byte>();

        buffer.AddRange(Header.Original[0..4]); // ??
        buffer.AddRange(new byte[2]); // Make space for length
        buffer.AddRange(Header.Original[6..8]); // Add message type
        buffer.AddRange(Encoding.UTF8.GetBytes(LoggerId)); // 10 Bytes
        buffer.AddRange(new byte[20]); // Random space??
        buffer.AddRange(BitConverter.GetBytes(Register).Reverse());
        buffer.AddRange(BitConverter.GetBytes((short)Value.Length).Reverse());
        buffer.AddRange(Value);

        return buffer.ToArray();
    }

    public static GrowattDataloggerCommandTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        return new ByteDecoder<GrowattDataloggerCommandTelegram>(new GrowattDataloggerCommandTelegram() { Header = header }, bytes)
            .Result;
    }
}

public static class GrowattDataloggerCommandTelegramExtensions
{
    public static GrowattDataloggerCommandTelegram CreateTimeCommand(this GrowattDataloggerCommandTelegram telegram, LocalDateTime dateTime)
    {
        telegram.Register = (ushort)GrowattDataloggerRegisters.TIME;
        telegram.Value = Encoding.UTF8.GetBytes(dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        return telegram;
    }
}
