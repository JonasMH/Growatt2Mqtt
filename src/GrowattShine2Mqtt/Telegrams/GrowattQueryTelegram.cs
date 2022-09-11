using System.Text;

namespace GrowattShine2Mqtt.Telegrams;

public class GrowattQueryTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattQueryTelegram()
    {
        Header = new GrowattTelegramHeader()
        {
            Original = new byte[8]
            {
                0x00, 0x01, 0x00, 0x06, 0x00, 0x00, 0x01, 0x19
            }
        };
    }

    public GrowattTelegramHeader Header { get; set; }
    public string LoggerId { get; set; }
    public short FirstItem { get; set; }
    public short LastItem { get; set; }

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
        buffer.AddRange(BitConverter.GetBytes(FirstItem).Reverse());
        buffer.AddRange(BitConverter.GetBytes(LastItem).Reverse());

        return buffer.ToArray();
    }


}
