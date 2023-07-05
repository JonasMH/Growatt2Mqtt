namespace GrowattShine2Mqtt.Telegrams;

public class GrowattTelegramHeader
{
    // In : 00 25 00 06 02 41 01 03 0d 22 2c 402040467734257761...
    // Out: 00 25 00 06 00 03 01 03 47 F7 D9
    //      ?? ?? ?? ?? ll ll tt tt data crc crc
    // ll ll = tt + data length
    // tt tt = message type
    // 
    // I just call the 4 first protocol version

    public uint ProtocolVersion {
        get {
            return GrowattByteDecoder.Instance.ReadUInt32(Bytes, 0);
        }
        init
        {
            Array.Copy(GrowattByteDecoder.Instance.WriteUInt32(value), 0, Bytes, 0, 4);
        }
    }

    public short MessageTypeRaw {
        get {
            return GrowattByteDecoder.Instance.ReadInt16(Bytes, 6);
        }
        init {
            Array.Copy(GrowattByteDecoder.Instance.WriteInt16(value), 0, Bytes, 6, 2);
        }
    }

    public short MessageLength
    {
        get
        {
            return GrowattByteDecoder.Instance.ReadInt16(Bytes, 4);
        }
        init
        {
            Array.Copy(GrowattByteDecoder.Instance.WriteInt16(value), 0, Bytes, 4, 2);
        }
    }

    public GrowattTelegramType? MessageType {
        get {
            return Enum.IsDefined(typeof(GrowattTelegramType), (int)MessageTypeRaw) ? (GrowattTelegramType)MessageTypeRaw : null;
        }
        init
        {
            if(!value.HasValue)
            {
                throw new ArgumentNullException("value not allowed to be null");
            }

            MessageTypeRaw = (short)value;
        }
    }
    public byte[] Bytes { get; }

    public GrowattTelegramHeader()
    {
        Bytes = new byte[8] {
            0x00, 0x01, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00
        };
    }
    public GrowattTelegramHeader(byte[] bytes)
    {
        if(bytes.Length != 8)
        {
            throw new ArgumentException("bytes are not allowed to be any lenght other than 8 bytes", nameof(bytes));
        }

        Bytes = bytes;
    }

    public static GrowattTelegramHeader Parse(ArraySegment<byte> bytes)
    {
        return new GrowattByteDecoderBuilder<GrowattTelegramHeader>(new GrowattTelegramHeader { }, bytes)
            .ReadUInt32(x => x.ProtocolVersion, 0)
            .ReadInt16(x => x.MessageLength, 4)
            .ReadInt16(x => x.MessageTypeRaw, 6)
            .Result;
    }
}
