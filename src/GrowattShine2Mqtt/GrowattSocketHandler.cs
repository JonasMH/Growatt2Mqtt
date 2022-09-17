using System;
using System.Net.Sockets;
using GrowattShine2Mqtt.Telegrams;
using NodaTime;

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

public class GrowattDataloggerInformation {
    public string? DataloggerSerial { get; set; }
    public Dictionary<ushort, byte[]> DataloggerRegisterValues { get; set; } = new();
    public Dictionary<ushort, ushort> InverterRegisterValues { get; set; } = new();
}

public class GrowattSocketHandler
{
    private readonly ILogger<GrowattSocketHandler> _logger;
    private readonly IGrowattToMqttHandler _growattToMqttHandler;
    private readonly IGrowattTelegramParser _telegramParser;
    private readonly IGrowattMetrics _metrics;
    private readonly IClock _systemClock;
    private readonly IDateTimeZoneProvider _timeZoneProvider;
    private readonly IGrowattSocket _socket;

    public GrowattDataloggerInformation Info { get; } = new();

    public GrowattSocketHandler(
        ILogger<GrowattSocketHandler> logger,
        IGrowattToMqttHandler growattToMqttHandler,
        IGrowattTelegramParser growattTelegramParser,
        IGrowattMetrics metrics,
        IClock systemClock,
        IDateTimeZoneProvider timeZoneProvider,
        IGrowattSocket socket)
    {
        _logger = logger;
        _growattToMqttHandler = growattToMqttHandler;
        _telegramParser = growattTelegramParser;
        _metrics = metrics;
        _systemClock = systemClock;
        _timeZoneProvider = timeZoneProvider;
        _socket = socket;
    }

    public async Task HandleMessageAsync(ArraySegment<byte> buffer)
    {
        var header = GrowattTelegramHeader.Parse(buffer);
        var telegram = _telegramParser.ParseMessage(buffer);

        _logger.LogInformation("Received {packageTypeRaw}({packetType}) packet of size {size} bytes v{protocolVersion}", header.MessageTypeRaw.ToHex(), header.MessageType, buffer.Count, header.ProtocolVersion);
        _logger.LogInformation("{packet}", buffer.ToHex());

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
            case GrowattDataloggerQueryResponseTelegram cmdResponseTelegram:
                _logger.LogInformation("Datalogger register {register}={data}", cmdResponseTelegram.Register.ToHex(), cmdResponseTelegram.Data.ToHex());
                Info.DataloggerRegisterValues.AddOrUpdate(cmdResponseTelegram.Register, cmdResponseTelegram.Data);
                break;
            case GrowattInverterCommandResponseTelegram inverterCommandResponse:
                _logger.LogInformation("Inverter register {register}={data}", inverterCommandResponse.Register.ToHex(), inverterCommandResponse.Data.ToHex());
                Info.InverterRegisterValues.AddOrUpdate(inverterCommandResponse.Register, inverterCommandResponse.Data);
                break;
            case GrowattInverterQueryResponseTelegram inverterQueryResponse:
                _logger.LogInformation("Inverter register {register}={data}", inverterQueryResponse.Register.ToHex(), inverterQueryResponse.Data.ToHex());
                Info.InverterRegisterValues.AddOrUpdate(inverterQueryResponse.Register, inverterQueryResponse.Data);
                break;
            case GrowattSPHData3Telegram data3Telegram:
                _logger.LogInformation("We seem to have received a data telegram from {date}, ACKing... \n{telegram}", data3Telegram.Date, data3Telegram);
                Info.DataloggerSerial = data3Telegram.Datalogserial;

                await SendTelegramAsync(new GrowattSPHData3TelegramAck(telegram.Header));

                await SendTelegramAsync(new GrowattDataloggerCommandTelegram()
                {
                    LoggerId = data3Telegram.Datalogserial
                }.CreateTimeCommand(_systemClock.GetCurrentInstant().InZone(_timeZoneProvider.GetSystemDefault()).LocalDateTime));
                break;
            case GrowattSPHData4Telegram data4Telegram:
                _logger.LogInformation("We seem to have received a data telegram from {date}, ACKing... \n{telegram}", data4Telegram.Date, data4Telegram);
                await SendTelegramAsync(new GrowattSPHData4TelegramAck(telegram.Header));
                _growattToMqttHandler.NewDataTelegram(data4Telegram);
                break;
        }
    }

    public async Task SendTelegramAsync(ISerializeableGrowattTelegram telegram)
    {
        var buffer = _telegramParser.PackMessage(telegram);
        _logger.LogInformation("Sending {telegramType}: {telegramData}", telegram.GetType().Name, buffer.ToHex());
        _metrics.MessageSent(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);
        await _socket.SendAsync(buffer);
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


public static class DictionaryExtensions
{
    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value)
    {
        if(dic.ContainsKey(key))
        {
            dic[key] = value;
        } else
        {
            dic.Add(key, value);
        }
    }
}
