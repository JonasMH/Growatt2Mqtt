using System;
using System.Net.Sockets;
using GrowattShine2Mqtt.Telegrams;

namespace GrowattShine2Mqtt;

public interface IGrowattSocket
{
    int Available { get; }
    Task SendAsync(ArraySegment<byte> buffer);
    Task<int> ReceiveAsync(ArraySegment<byte> buffer);
}

public class GrowattSocket : IGrowattSocket
{
    private readonly Socket _socket;

    public GrowattSocket(Socket socket)
    {
        _socket = socket;
    }

    public int Available => _socket.Available;

    public async Task SendAsync(ArraySegment<byte> buffer)
    {
        await _socket.SendAsync(buffer, SocketFlags.None);
    }

    public async Task<int> ReceiveAsync(ArraySegment<byte> buffer)
    {
        return await _socket.ReceiveAsync(buffer, SocketFlags.None);
    }
}

public class GrowattSocketHandler
{
    private readonly ILogger<GrowattSocketHandler> _logger;
    private readonly IGrowattToMqttHandler _growattToMqttHandler;
    private readonly IGrowattTelegramParser _telegramParser;
    private readonly IGrowattMetrics _metrics;
    private readonly IGrowattSocket _socket;

    public GrowattSocketHandler(
        ILogger<GrowattSocketHandler> logger,
        IGrowattToMqttHandler growattToMqttHandler,
        IGrowattTelegramParser growattTelegramParser,
        IGrowattMetrics metrics,
        IGrowattSocket socket)
    {
        _logger = logger;
        _growattToMqttHandler = growattToMqttHandler;
        _telegramParser = growattTelegramParser;
        _metrics = metrics;
        _socket = socket;
    }

    public async Task HandleMessageAsync(ArraySegment<byte> buffer)
    {
        var telegram = _telegramParser.ParseMessage(buffer);

        _logger.LogInformation("Received {packageTypeRaaw}({packetType}) packet of size {size} bytes v{protocolVersion}", telegram?.Header.MessageTypeRaw, telegram?.Header.MessageType, buffer.Count, telegram?.Header.ProtocolVersion);
        _logger.LogDebug("{packet}", buffer.ToHex());

        if (telegram == null)
        {
            return;
        }

        _metrics.MessageReceived(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);

        switch (telegram)
        {
            case GrowattSPHPingTelegram pingTelegram:
                _logger.LogInformation("We seem to have received a ping, echoing...");
                _metrics.MessageSent(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);
                await _socket.SendAsync(buffer);
                break;
            case GrowattSPHData3Telegram data3Telegram:
                _logger.LogInformation("We seem to have received a data telegram from {date}, ACKing... \n{telegram}", data3Telegram.Date, data3Telegram);
                var data3Response = new GrowattSPHData3TelegramAck(telegram.Header);
                var data3Bytes = data3Response.ToBytes();
                _metrics.MessageSent(telegram.Header.MessageType?.ToString() ?? "", data3Bytes.Length);
                await _socket.SendAsync(data3Bytes);
                break;
            case GrowattSPHData4Telegram data4Telegram:
                _logger.LogInformation("We seem to have received a data telegram from {date}, ACKing... \n{telegram}", data4Telegram.Date, data4Telegram);
                var data4Response = new GrowattSPHData4TelegramAck(telegram.Header);
                var data4Bytes = data4Response.ToBytes();
                _metrics.MessageSent(telegram.Header.MessageType?.ToString() ?? "", data4Bytes.Length);
                await _socket.SendAsync(data4Bytes);
                _growattToMqttHandler.NewDataTelegram(data4Telegram);
                break;
        }
    }

    public async Task RunAsync()
    {
        byte[] buffer = new byte[1024 * 8];
        while (true)
        {
            if (_socket.Available <= 0)
            {
                await Task.Delay(5);
                continue;
            }

            try
            {
                var amountReceived = await _socket.ReceiveAsync(buffer);
                await HandleMessageAsync(new ArraySegment<byte>(buffer, 0, amountReceived));

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to handle socket");
                return;
            }
        }
    }
}
