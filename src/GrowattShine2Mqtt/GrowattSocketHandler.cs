using GrowattShine2Mqtt.Telegrams;
using NodaTime;

namespace GrowattShine2Mqtt;

public class GrowattSocketHandler {
    private readonly object _queryLock = new();
    private TaskCompletionSource<GrowattInverterQueryResponseTelegram>? _pendingInverterQuery;

    private readonly ILogger<GrowattSocketHandler> _logger;
    private readonly IGrowattToMqttHandler _growattToMqttHandler;
    private readonly IGrowattTelegramParser _telegramParser;
    private readonly GrowattMetrics _metrics;
    private readonly IClock _systemClock;
    private readonly IDateTimeZoneProvider _timeZoneProvider;
    private readonly IGrowattSocket _socket;

    [ActivatorUtilitiesConstructor]
    public GrowattSocketHandler(
        ILogger<GrowattSocketHandler> logger,
        IGrowattToMqttHandler growattToMqttHandler,
        IGrowattTelegramParser growattTelegramParser,
        GrowattMetrics metrics,
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

    public GrowattDataloggerInformation Info { get; } = new();

    public async Task HandleMessageAsync(ArraySegment<byte> buffer)
    {
        var header = GrowattTelegramHeader.Parse(buffer);
        var telegram = _telegramParser.ParseMessage(buffer);

        _logger.LogInformation("Received {packageTypeRaw}({packetType}) packet of size {size} bytes v{protocolVersion}", header.MessageTypeRaw.ToString("X"), header.MessageType, buffer.Count, header.ProtocolVersion);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("0x{packet}", Convert.ToHexStringLower(buffer));
        }

        if (telegram == null)
        {
            return;
        }

        _metrics.MessageReceived(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);

        switch (telegram)
        {
            case GrowattSPHPingTelegram _:
                _logger.LogInformation("Received a ping, echoing...");
                _metrics.MessageSent(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);
                await _socket.SendAsync(buffer);
                break;
            case GrowattDataloggerQueryResponseTelegram cmdResponseTelegram:
                _logger.LogInformation("Datalogger register {register}=0x{data}", cmdResponseTelegram.Register, Convert.ToHexStringLower(cmdResponseTelegram.Data));
                Info.DataloggerRegisterValues.AddOrUpdate(cmdResponseTelegram.Register, cmdResponseTelegram.Data);
                break;
            case GrowattInverterCommandResponseTelegram inverterCommandResponse:
                _logger.LogInformation("Inverter register (Result={result}) {register}=0x{dataHex} ({data})", inverterCommandResponse.Result, inverterCommandResponse.Register, inverterCommandResponse.Data.ToString("X"), inverterCommandResponse.Data);
                Info.InverterRegisterValues.AddOrUpdate(inverterCommandResponse.Register, inverterCommandResponse.Data);
                break;
            case GrowattInverterQueryResponseTelegram inverterQueryResponse:
                _logger.LogInformation("Inverter register {register}=0x{dataHex} ({data})", inverterQueryResponse.Register, inverterQueryResponse.Data.ToString("X"), inverterQueryResponse.Data);
                Info.InverterRegisterValues.AddOrUpdate(inverterQueryResponse.Register, inverterQueryResponse.Data);
                // Complete pending query if any
                TaskCompletionSource<GrowattInverterQueryResponseTelegram>? tcs = null;
                lock (_queryLock)
                {
                    tcs = _pendingInverterQuery;
                    _pendingInverterQuery = null;
                }
                tcs?.TrySetResult(inverterQueryResponse);
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
                await _growattToMqttHandler.HandleDataTelegramAsync(data4Telegram);
                break;
        }
    }


    /// <summary>
    /// Sends a GrowattInverterQueryRequestTelegram and waits for the corresponding GrowattInverterQueryResponseTelegram.
    /// </summary>
    /// <param name="request">The request telegram to send.</param>
    /// <param name="token">CancellationToken for timeout/cancel.</param>
    /// <returns>The awaited GrowattInverterQueryResponseTelegram.</returns>
    public async Task<GrowattInverterQueryResponseTelegram> QueryInverterRegister(GrowattInverterQueryRequestTelegram request, CancellationToken token)
    {
        TaskCompletionSource<GrowattInverterQueryResponseTelegram> tcs;
        lock (_queryLock)
        {
            if (_pendingInverterQuery != null)
                throw new InvalidOperationException("Another inverter query is already pending.");
            _pendingInverterQuery = new(TaskCreationOptions.RunContinuationsAsynchronously);
            tcs = _pendingInverterQuery;
        }
        try
        {
            await SendTelegramAsync(request);
            using (token.Register(() => tcs.TrySetCanceled(token), useSynchronizationContext: false))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            lock (_queryLock)
            {
                _pendingInverterQuery = null;
            }
        }
    }

    public async Task SendTelegramAsync(ISerializeableGrowattTelegram telegram)
    {
        var buffer = _telegramParser.PackMessage(telegram);

        if(_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Sending {telegramType}: 0x{telegramData}", telegram.GetType().Name, Convert.ToHexStringLower(buffer));
        } else
        {
            _logger.LogInformation("Sending {telegramType}: {content}", telegram.GetType().Name, telegram.ToString());
        }

        _metrics.MessageSent(telegram.Header.MessageType?.ToString() ?? "", buffer.Count);
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
                _logger.LogError(e, "Failed to handle socket {socketid}. Data: {data}", _socket.SocketId, Convert.ToHexStringLower(received));
                return;
            }
        }
    }
}


public static class DictionaryExtensions
{
    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value) where TKey : notnull
    {
        if (!dic.TryAdd(key, value))
        {
            dic[key] = value;
        }
    }
}
