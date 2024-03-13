using System.Net.Sockets;
using GrowattShine2Mqtt.Telegrams;
using NodaTime;

namespace GrowattShine2Mqtt;

public interface IGrowattSocket
{
    int Available { get; }
    int SocketId { get; }
    bool Connected { get; }
    Task SendAsync(ArraySegment<byte> buffer);
    Task<int> ReceiveAsync(ArraySegment<byte> buffer);
}

public class GrowattSocket(Socket socket, int socketId) : IGrowattSocket
{
    private readonly Socket _socket = socket;
    private readonly int _socketId = socketId;

    public int Available => _socket.Available;
    public int SocketId => _socketId;
    public bool Connected => _socket.Connected;

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
    public Dictionary<ushort, byte[]> DataloggerRegisterValues { get; set; } = [];
    public Dictionary<ushort, ushort> InverterRegisterValues { get; set; } = [];
}

public class GrowattSocketHandler(
    ILogger<GrowattSocketHandler> logger,
    IGrowattToMqttHandler growattToMqttHandler,
    IGrowattTelegramParser growattTelegramParser,
    GrowattMetrics? metrics,
    IClock systemClock,
    IDateTimeZoneProvider timeZoneProvider,
    IGrowattSocket socket)
{
    private readonly ILogger<GrowattSocketHandler> _logger = logger;
    private readonly IGrowattToMqttHandler _growattToMqttHandler = growattToMqttHandler;
    private readonly IGrowattTelegramParser _telegramParser = growattTelegramParser;
    private readonly GrowattMetrics? _metrics = metrics;
    private readonly IClock _systemClock = systemClock;
    private readonly IDateTimeZoneProvider _timeZoneProvider = timeZoneProvider;
    private readonly IGrowattSocket _socket = socket;

    public GrowattDataloggerInformation Info { get; } = new();

    public async Task HandleMessageAsync(ArraySegment<byte> buffer)
    {
        var header = GrowattTelegramHeader.Parse(buffer);
        var telegram = _telegramParser.ParseMessage(buffer);

        _logger.LogInformation("Received {packageTypeRaw}({packetType}) packet of size {size} bytes v{protocolVersion}", header.MessageTypeRaw.ToHex(), header.MessageType, buffer.Count, header.ProtocolVersion);
        if(_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("0x{packet}", buffer.ToHex());
        }

        if (telegram == null)
        {
            return;
        }

        _metrics?.MessageReceived(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);

        switch (telegram)
        {
            case GrowattSPHPingTelegram _:
                _logger.LogInformation("Received a ping, echoing...");
                _metrics?.MessageSent(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);
                await _socket.SendAsync(buffer);
                break;
            case GrowattDataloggerQueryResponseTelegram cmdResponseTelegram:
                _logger.LogInformation("Datalogger register {register}=0x{data}", cmdResponseTelegram.Register, cmdResponseTelegram.Data.ToHex());
                Info.DataloggerRegisterValues.AddOrUpdate(cmdResponseTelegram.Register, cmdResponseTelegram.Data);
                break;
            case GrowattInverterCommandResponseTelegram inverterCommandResponse:
                _logger.LogInformation("Inverter register (Result={result}) {register}={data}/{dataHex}", inverterCommandResponse.Result, inverterCommandResponse.Register, inverterCommandResponse.Data, inverterCommandResponse.Data.ToHex());
                Info.InverterRegisterValues.AddOrUpdate(inverterCommandResponse.Register, inverterCommandResponse.Data);
                break;
            case GrowattInverterQueryResponseTelegram inverterQueryResponse:
                _logger.LogInformation("Inverter register {register}={data}/{dataHex}", inverterQueryResponse.Register, inverterQueryResponse.Data, inverterQueryResponse.Data.ToHex());
                Info.InverterRegisterValues.AddOrUpdate(inverterQueryResponse.Register, inverterQueryResponse.Data);
                break;
            case GrowattSPHData3Telegram data3Telegram:
                _logger.LogInformation("Received data3 telegram from {serial}, ACKing...", data3Telegram.Datalogserial);
                Info.DataloggerSerial = data3Telegram.Datalogserial;

                await SendTelegramAsync(new GrowattSPHData3TelegramAck(telegram.Header));

                await SendTelegramAsync(new GrowattDataloggerCommandTelegram()
                {
                    LoggerId = data3Telegram.Datalogserial
                }.CreateTimeCommand(_systemClock.GetCurrentInstant().InZone(_timeZoneProvider.GetSystemDefault()).LocalDateTime));
                break;
            case GrowattSPHData4Telegram data4Telegram:
                _logger.LogInformation("Received a data4 telegram from {date}, ACKing...", data4Telegram.Date);
                await SendTelegramAsync(new GrowattSPHData4TelegramAck(telegram.Header));
                _growattToMqttHandler.NewDataTelegram(data4Telegram);
                break;
        }
    }

    public async Task SendTelegramAsync(ISerializeableGrowattTelegram telegram)
    {
        var buffer = _telegramParser.PackMessage(telegram);
        _logger.LogDebug("Sending {telegramType}: 0x{telegramData}", telegram.GetType().Name, buffer.ToHex());
        _metrics?.MessageSent(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);
        await _socket.SendAsync(buffer);
    }

    public async Task RunAsync(CancellationToken token)
    {
        byte[] buffer = new byte[1024 * 8];
        while (!token.IsCancellationRequested && _socket.Connected)
        {
            if (_socket.Available <= 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(5), token);
                continue;
            }

            if(!_socket.Connected)
            {
                _logger.LogInformation("Socket {socketId} no longer connected", _socket.SocketId);
                return;
            }

            var received = new ArraySegment<byte>();
            try
            {
                var amountReceived = await _socket.ReceiveAsync(buffer);
                received = new ArraySegment<byte>(buffer, 0, amountReceived);
                await HandleMessageAsync(received);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to handle socket {socketid}. Data: {data}", _socket.SocketId, received.ToHex());
                return;
            }
        }
    }
}


public static class DictionaryExtensions
{
    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value) where TKey : notnull
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
