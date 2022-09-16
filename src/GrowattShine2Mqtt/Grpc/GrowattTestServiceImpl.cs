using Google.Protobuf;
using GrowattShine2Mqtt.Schema;
using GrowattShine2Mqtt.Telegrams;
using Grpc.Core;

namespace GrowattShine2Mqtt.Grpc;

public class GrowattTestServiceImpl : GrowattTestService.GrowattTestServiceBase
{
    private readonly ILogger<GrowattTestServiceImpl> _logger;
    private readonly IGrowattServerListener _serverListener;

    public GrowattTestServiceImpl(ILogger<GrowattTestServiceImpl> logger, IGrowattServerListener serverListener)
    {
        _logger = logger;
        _serverListener = serverListener;
    }

    public override async Task<CommandDataLoggerResponse> CommandDatalogger(CommandDataLoggerRequest request, ServerCallContext context)
    {
        var loggerSocket = _serverListener.Sockets.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if(loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        await loggerSocket.SendTelegramAsync(new GrowattDataloggerCommandTelegram()
        {
            LoggerId = request.Datalogger,
            Register = (ushort)request.Register,
            Value = request.Value.ToArray().Reverse().ToArray()
        });

        return new CommandDataLoggerResponse();
    }

    public override async Task<CommandInverterResponse> CommandInverter(CommandInverterRequest request, ServerCallContext context)
    {
        var loggerSocket = _serverListener.Sockets.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        await loggerSocket.SendTelegramAsync(new GrowattInverterCommandTelegram()
        {
            LoggerId = request.Datalogger,
            Register = (ushort)request.Register,
            Value = BitConverter.ToUInt16(request.Value.ToArray())
        });

        return new CommandInverterResponse();
    }

    public override Task<GetDataLoggerInfoResponse> GetDataLoggerInfo(GetDataLoggerInfoRequest request, ServerCallContext context)
    {
        var loggerSocket = _serverListener.Sockets.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }
        var response = new GetDataLoggerInfoResponse();

        response.Datalogger = request.Datalogger;

        foreach (var dataloggerRegister in loggerSocket.Info.DataloggerRegisterValues)
        {
            response.DataloggerRegisters.Add(dataloggerRegister.Key, ByteString.CopyFrom(dataloggerRegister.Value));
        }

        foreach (var inverterRegister in loggerSocket.Info.InverterRegisterValues)
        {
            response.InverterRegisters.Add(inverterRegister.Key, ByteString.CopyFrom(BitConverter.GetBytes(inverterRegister.Value)));
        }


        return Task.FromResult(response);
    }

    public override Task<ListDataloggersResponse> ListDataloggers(ListDataloggersRequest request, ServerCallContext context)
    {
        var response = new ListDataloggersResponse();

        response.Dataloggers.AddRange(_serverListener.Sockets.Select(x => x.Info.DataloggerSerial));

        return Task.FromResult(response);
    }

    public override async Task<QueryDataLoggerResponse> QueryDatalogger(QueryDataLoggerRequest request, ServerCallContext context)
    {
        var loggerSocket = _serverListener.Sockets.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        await loggerSocket.SendTelegramAsync(new GrowattDataloggerQueryTelegram()
        {
            LoggerId = request.Datalogger,
            StartingAddress = (ushort)request.StartRegister,
            EndAddress = (ushort)request.EndRegister
        });

        return new QueryDataLoggerResponse();
    }

    public override async Task<QueryInverterResponse> QueryInverter(QueryInverterRequest request, ServerCallContext context)
    {
        var loggerSocket = _serverListener.Sockets.FirstOrDefault(x => x.Info.DataloggerSerial == request.Datalogger);

        if (loggerSocket == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Datalogger {request.Datalogger} wasn't found"));
        }

        await loggerSocket.SendTelegramAsync(new GrowattInverterQueryTelegram()
        {
            LoggerId = request.Datalogger,
            StartingAddress = (ushort)request.StartRegister,
            EndAddress = (ushort)request.EndRegister
        });

        return new QueryInverterResponse();
    }
}
