using System.Net.Sockets;
using GrowattShine2Mqtt.Telegrams;

namespace GrowattShine2Mqtt;

public class GrowattSocketHandler
{
    private readonly ILogger<GrowattSocketHandler> _logger;
    private readonly IGrowattToMqttHandler _growattToMqttHandler;
    private readonly GrowattTelegramParser _telegramParser;
    private readonly GrowattMetrics _metrics;
    private readonly Socket _socket;

    public GrowattSocketHandler(
        ILogger<GrowattSocketHandler> logger,
        IGrowattToMqttHandler growattToMqttHandler,
        GrowattTelegramParser growattTelegramParser,
        GrowattMetrics metrics,
        Socket socket)
    {
        _logger = logger;
        _growattToMqttHandler = growattToMqttHandler;
        _telegramParser = growattTelegramParser;
        _metrics = metrics;
        _socket = socket;
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
                var amountReceived = await _socket.ReceiveAsync(buffer, SocketFlags.None);

                _logger.LogInformation("Received packet of size {size}: {body}", amountReceived, BitConverter.ToString(buffer, 0, amountReceived));

                var telegram = _telegramParser.HandleMessage(new ArraySegment<byte>(buffer, 0, amountReceived));

                if (telegram == null)
                {
                    continue;
                }

                _metrics.MessageReceived(telegram.Header.MessageType?.ToString() ?? "", amountReceived);

                switch (telegram)
                {
                    case GrowattSPHPingTelegram pingTelegram:
                        _logger.LogInformation("We seem to have received a ping, echoing...");
                        await _socket.SendAsync(new ArraySegment<byte>(buffer, 0, amountReceived), SocketFlags.None);
                        break;
                    case GrowattSPHData4Telegram dataTelegram:
                        _logger.LogInformation("We seem to have received a data telegram, ACKing...");
                        var response = new byte[]
                        {
                                0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x01, 0x04, 0x00
                        };
                        _metrics.MessageReceived(telegram.Header.MessageType?.ToString() ?? "", response.Length);
                        await _socket.SendAsync(response, SocketFlags.None);
                        _growattToMqttHandler.NewDataTelegram(dataTelegram);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to handle socket");
                return;
            }
        }
    }
}
