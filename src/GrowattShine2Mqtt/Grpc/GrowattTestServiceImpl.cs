using Google.Protobuf;
using GrowattShine2Mqtt.Schema;
using GrowattShine2Mqtt.Telegrams;
using Grpc.Core;

namespace GrowattShine2Mqtt.Grpc;

public class GrowattTestServiceImpl(ILogger<GrowattTestServiceImpl> logger, GrowattServerListener serverListener) : GrowattTestService.GrowattTestServiceBase
{
    private readonly ILogger<GrowattTestServiceImpl> _logger = logger;
    private readonly GrowattServerListener _serverListener = serverListener;

    public override async Task<CommandDataLoggerResponse> CommandDatalogger(CommandDataLoggerRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Commanding datalogger");
        var loggerSocket = _serverListener.Sockets.Values.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if(loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        var telegram = new GrowattDataloggerCommandTelegram()
        {
            LoggerId = request.Datalogger,
            Register = (ushort)request.Register,
            Value = request.Value.ToArray().Reverse().ToArray()
        };

        _logger.LogInformation("Commanding datalogger {datalogger} setting register {registerAddr} to {registerValue}", telegram.LoggerId, telegram.Register, telegram.Value);


        await loggerSocket.SendTelegramAsync(telegram);

        return new CommandDataLoggerResponse();
    }

    public override async Task<CommandInverterResponse> CommandInverter(CommandInverterRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Commanding inverter");
        var loggerSocket = _serverListener.Sockets.Values.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        var telegram = new GrowattInverterCommandTelegram()
        {
            DataloggerId = request.Datalogger,
            Register = (ushort)request.Register,
            Value = (ushort)request.Value
        };

        _logger.LogInformation("Commanding inverter {datalogger} setting register {registerAddr} to {registerValue}", telegram.DataloggerId, telegram.Register, telegram.Value);

        await loggerSocket.SendTelegramAsync(telegram);

        return new CommandInverterResponse();
    }

    public override Task<GetDataLoggerInfoResponse> GetDataLoggerInfo(GetDataLoggerInfoRequest request, ServerCallContext context)
    {
        var loggerSocket = _serverListener.Sockets.Values.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }
        var response = new GetDataLoggerInfoResponse
        {
            Datalogger = request.Datalogger
        };

        foreach (var dataloggerRegister in loggerSocket.Info.DataloggerRegisterValues)
        {
            response.DataloggerRegisters.Add(dataloggerRegister.Key, ByteString.CopyFrom(dataloggerRegister.Value));
        }

        foreach (var inverterRegister in loggerSocket.Info.InverterRegisterValues)
        {
            response.InverterRegisters.Add(inverterRegister.Key, inverterRegister.Value);
        }


        return Task.FromResult(response);
    }

    public override Task<ListDataloggersResponse> ListDataloggers(ListDataloggersRequest request, ServerCallContext context)
    {
        var response = new ListDataloggersResponse();

        response.Dataloggers.AddRange(_serverListener.Sockets.Select(x => x.Value.Info.DataloggerSerial));

        return Task.FromResult(response);
    }

    public override async Task<QueryDataLoggerResponse> QueryDatalogger(QueryDataLoggerRequest request, ServerCallContext context)
    {
        var loggerSocket = _serverListener.Sockets.Values.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        var telegram = new GrowattDataloggerQueryTelegram()
        {
            LoggerId = request.Datalogger,
            StartAddress = (ushort)request.StartRegister,
            EndAddress = (ushort)request.EndRegister
        };

        _logger.LogInformation("Querying datalogger {datalogger} registers {startRegister} to {endRegister}", telegram.LoggerId, telegram.StartAddress, telegram.EndAddress);

        await loggerSocket.SendTelegramAsync(telegram);

        return new QueryDataLoggerResponse();
    }

    public override async Task<QueryInverterResponse> QueryInverter(QueryInverterRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Query inverter");
        var loggerSocket = _serverListener.Sockets.Values.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        var telegram = new GrowattInverterQueryRequestTelegram()
        {
            DataloggerId = request.Datalogger,
            StartAddress = (ushort)request.StartRegister,
            EndAddress = (ushort)request.EndRegister
        };

        _logger.LogInformation("Querying inverter {datalogger} registers {startRegister} to {endRegister}", telegram.DataloggerId, telegram.StartAddress, telegram.EndAddress);

        await loggerSocket.SendTelegramAsync(telegram);

        return new QueryInverterResponse();
    }
}
