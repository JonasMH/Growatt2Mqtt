using System.Globalization;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace GrowattShine2Mqtt.Telegrams;

public enum GrowattConfigureRegisters
{
    TIME = 0x001F,
    SERVER_ADDRESS = 0x0013,
    REBOOT = 0x0020
}

public class GrowattConfigureTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattConfigureTelegram()
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
    public short Register { get; set; }
    public byte[] Value { get; set; }


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
        buffer.AddRange(BitConverter.GetBytes(Register).Reverse());
        buffer.AddRange(BitConverter.GetBytes((short)Value.Length).Reverse());
        buffer.AddRange(Value);

        return buffer.ToArray();
    }

    public static GrowattConfigureTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        return new ByteDecoder<GrowattConfigureTelegram>(new GrowattConfigureTelegram() { Header = header }, bytes)
            .Result;
    }
}

public static class GrowattConfigureTelegramExtensions
{
    public static GrowattConfigureTelegram CreateTimeCommand(this GrowattConfigureTelegram telegram, Instant instant)
    {
        telegram.Register = (short)GrowattConfigureRegisters.TIME;
        telegram.Value = Encoding.UTF8.GetBytes(instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        return telegram;
    }
}
