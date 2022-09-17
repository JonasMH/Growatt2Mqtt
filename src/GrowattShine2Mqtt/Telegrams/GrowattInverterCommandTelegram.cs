using System.Text;

namespace GrowattShine2Mqtt.Telegrams;


public enum GrowattInveterRegisters
{

}

public class GrowattInverterCommandTelegram : IGrowattTelegram, ISerializeableGrowattTelegram
{
    public GrowattInverterCommandTelegram()
    {
        Header = new GrowattTelegramHeader()
        {
            Original = new byte[8]
            {
                0x00, 0x01, 0x00, 0x06, 0x00, 0x00, 0x01, 0x06
            }
        };
    }

    public GrowattTelegramHeader Header { get; set; }

    public string LoggerId { get; set; }
    public ushort Register { get; set; }
    public ushort Value { get; set; }


    public byte[] ToBytes()
    {
        var buffer = new List<byte>();

        buffer.AddRange(Header.Original[0..4]); // ??
        buffer.AddRange(new byte[2]); // Make space for length
        buffer.AddRange(Header.Original[6..8]); // Add message type
        buffer.AddRange(Encoding.UTF8.GetBytes(LoggerId)); // 10 Bytes
        buffer.AddRange(new byte[20]); // Random space??
        buffer.AddRange(BitConverter.GetBytes(Register).Reverse());
        buffer.AddRange(BitConverter.GetBytes(Value).Reverse());

        return buffer.ToArray();
    }
}

public class GrowattInverterCommandResponseTelegram : IGrowattTelegram
{
    public GrowattInverterCommandResponseTelegram(GrowattTelegramHeader header)
    {
        Header = header;
    }

    public GrowattTelegramHeader Header { get; set; }

    public string Datalogserial { get; set; }
    public byte Result { get; set; }
    public ushort Register { get; set; }
    public ushort Data { get; set; }

    public static GrowattInverterCommandResponseTelegram Parse(ArraySegment<byte> bytes, GrowattTelegramHeader header)
    {
        // In : 00 01 00 06 00 37 01 18 0d 22 ..
        //      ?? ?? ?? ?? ll ll tt tt rr rr
        // ll ll = tt + data length
        // tt tt = message type
        // rr rr = register

        var offset = 20;
        return new ByteDecoder<GrowattInverterCommandResponseTelegram>(new GrowattInverterCommandResponseTelegram(header), bytes)
            .ReadString(x => x.Datalogserial, 8, 10)
            .ReadUInt16(x => x.Register, 18 + offset)
            .ReadByte(x => x.Result, 20 + offset)
            .ReadUInt16(x => x.Data, 21 + offset)
            .Result;
    }
}
