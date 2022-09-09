using System.Net.NetworkInformation;
using GrowattShine2Mqtt.Telegrams;

namespace GrowattShine2Mqtt;

public interface IGrowattTelegramParser
{
    IGrowattTelegram? ParseMessage(ArraySegment<byte> buffer);
}

public class GrowattTelegramParser : IGrowattTelegramParser
{
    private readonly ILogger<GrowattTelegramParser> _logger;

    public GrowattTelegramParser(ILogger<GrowattTelegramParser> logger)
    {
        _logger = logger;
    }

    public byte[] Decrypt(ArraySegment<byte> buffer)
    {
        var dataLength = buffer.Count;
        var mask = "Growatt";
        var hexMask = mask.Select(x => ((int)x)).ToArray();
        var unscrambled = new byte[dataLength];

        // Copy header
        Array.Copy(buffer.Array, buffer.Offset, unscrambled, 0, 8);

        for (int i = 0; i < dataLength - 8; i++)
        {
            unscrambled[i + 8] = (byte)(buffer[i + 8] ^ (byte)hexMask[i % hexMask.Length]);
        }

        return unscrambled;
    }

    public IGrowattTelegram? ParseMessage(ArraySegment<byte> buffer)
    {
        var decrypted = Decrypt(buffer);

        var header = buffer[0..8];
        var parsedHeader = GrowattTelegramHeader.Parse(header);

        switch (parsedHeader.MessageType)
        {
            case GrowattTelegramType.PING:
                return GrowattSPHPingTelegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.DATA3:
                return GrowattSPHData3Telegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.DATA4:
                return GrowattSPHData4Telegram.Parse(decrypted, parsedHeader);
        }

        _logger.LogWarning("Failed to parse message of type {messageTypeRaw}", parsedHeader.MessageTypeRaw.ToHex());
        return null;
    }
}
