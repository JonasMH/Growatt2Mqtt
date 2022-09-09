namespace GrowattShine2Mqtt.Telegrams;

public class GrowattTelegramHeader
{
    public byte ProtocolVersion { get; set; }
    public short MessageTypeRaw { get; set; }
    public short MessageLength { get; set; }
    public GrowattTelegramType? MessageType => Enum.IsDefined(typeof(GrowattTelegramType), (int)MessageTypeRaw) ? (GrowattTelegramType)MessageTypeRaw : null;
    public byte[] Original { get; set; }

    public static GrowattTelegramHeader Parse(ArraySegment<byte> bytes)
    {
        return new ByteDecoder<GrowattTelegramHeader>(new GrowattTelegramHeader { Original = bytes[0..8].ToArray() }, bytes)
            .ReadByte(x => x.ProtocolVersion, 3)
            .ReadInt16(x => x.MessageLength, 4)
            .ReadInt16(x => x.MessageTypeRaw, 6)
            .Result;
    }
}
