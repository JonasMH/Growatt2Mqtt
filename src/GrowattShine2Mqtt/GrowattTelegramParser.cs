using System;
using System.Net.NetworkInformation;
using GrowattShine2Mqtt.Telegrams;

namespace GrowattShine2Mqtt;

public interface IGrowattTelegramParser
{
    IGrowattTelegram? ParseMessage(ArraySegment<byte> buffer);
    ArraySegment<byte> PackMessage(ISerializeableGrowattTelegram telegram);
}

public interface IGrowattTelegramEncrypter
{
    byte[] Decrypt(ArraySegment<byte> buffer);
}

public class GrowattTelegramEncrypter : IGrowattTelegramEncrypter
{
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
}

public class GrowattTelegramParser : IGrowattTelegramParser
{
    private readonly ILogger<GrowattTelegramParser> _logger;
    private readonly IGrowattTelegramEncrypter _encrypter;

    public GrowattTelegramParser(ILogger<GrowattTelegramParser> logger, IGrowattTelegramEncrypter encrypter)
    {
        _logger = logger;
        _encrypter = encrypter;
    }


    public ArraySegment<byte> PackMessage(ISerializeableGrowattTelegram telegram)
    {

        // In : 00 25 00 06 02 41 01 03 0d 22 2c 402040467734257761...
        // Out: 00 25 00 06 00 03 01 03 47 F7 D9
        //      ?? ?? ?? ?? ll ll tt tt data crc crc
        // ll ll = tt + data length
        // tt tt = message type

        var telegramBytes = telegram.ToBytes();
        var buffer = new byte[telegramBytes.Length + 2];// Make space for crc


        if(telegramBytes.Length > 8) // Has a body we need to encrypt
        {
            var encrypted = _encrypter.Decrypt(telegramBytes);
            Array.Copy(encrypted, 0, buffer, 0, encrypted.Length);
        } else
        {
            Array.Copy(telegramBytes, 0, buffer, 0, telegramBytes.Length);
        }

        // Write length
        Array.Copy(BitConverter.GetBytes((short)(buffer.Length - 8)).Reverse().ToArray(), 0, buffer, 4, 2);

        // Write CRC
        var crc = new Crc16Modbus();
        var crcResult = BitConverter.GetBytes(crc.ComputeChecksum(buffer[0..^2])).Reverse().ToArray();

        Array.Copy(crcResult, 0, buffer, buffer.Length - 2, 2);

        return buffer;
    }

    public IGrowattTelegram? ParseMessage(ArraySegment<byte> buffer)
    {
        var decrypted = _encrypter.Decrypt(buffer);

        var header = buffer[0..8];
        var parsedHeader = GrowattTelegramHeader.Parse(header);

        var crc = new Crc16Modbus();
        var expectedCrc = BitConverter.GetBytes(crc.ComputeChecksum(buffer[0..^2])).Reverse().ToArray();

        if (expectedCrc[0] != buffer[^2] || expectedCrc[1] != buffer[^1])
        {
            _logger.LogWarning("Skipping telegram due to CRC Check failed. Telegram {telgram}. Expected {expectedCrc} was {actualCrc}", buffer.ToHex(), expectedCrc.ToHex(), buffer[^2..].ToHex());
            return null;
        }

        switch (parsedHeader.MessageType)
        {
            case GrowattTelegramType.PING:
                return GrowattSPHPingTelegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.DATA3:
                return GrowattSPHData3Telegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.DATA4:
                return GrowattSPHData4Telegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.QUERY_DATALOGGER:
                return GrowattDataloggerQueryResponseTelegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.COMMAND_DATALOGGER:
                return GrowattDataloggerCommandTelegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.COMMAND_INVERTER:
                return GrowattInverterCommandResponseTelegram.Parse(decrypted, parsedHeader);
            case GrowattTelegramType.QUERY_INVERTER:
                return GrowattInverterQueryResponseTelegram.Parse(decrypted, parsedHeader);
        }

        _logger.LogWarning("Unable to handle telegrams of type {messageTypeRaw}", parsedHeader.MessageTypeRaw.ToHex());
        return null;
    }
}
