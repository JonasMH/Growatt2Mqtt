using GrowattShine2Mqtt.Telegrams;
using ToMqttNet;
using MQTTnet;
using NodaTime;
using System.Text.Json;
using HomeAssistantDiscoveryNet;
using MQTTnet.Client;

namespace GrowattShine2Mqtt;

public interface IGrowattToMqttHandler
{
    Task HandleDataTelegramAsync(GrowattSPHData4Telegram data4Telegram);
}

public static class GrowattInverterRegisters
{
    public static readonly GrowattInverterRegister ExportLimitEnableDisable = new(122, "ExportLimitEnableDisable");

    /// <summary>
    /// Range: -1000~+1000, Step size: 0.1%
    /// </summary>
    public static readonly GrowattInverterRegister ExportLimitPowerRate = new(123, "ExportLimitPowerRate");

    public static readonly GrowattInverterRegister BatteryFirstChargeStopSoC = new(1091, "BatteryFirstSoC");
    public static readonly GrowattInverterRegister BatteryFirstAcCharge = new(1092, "BatteryFirstAcCharge");
    public static readonly GrowattInverterRegister BatteryFirst1Start = new(1100, "BatteryFirst1Start");
    public static readonly GrowattInverterRegister BatteryFirst1Stop = new(1101, "BatteryFirst1Stop");
    public static readonly GrowattInverterRegister BatteryFirst1Enabled = new(1102, "BatteryFirst1Enabled");
    public static readonly GrowattInverterRegister LoadFirstStopSocSet = new(3082, "LoadFirstStopSocSet");
}

public record GrowattInverterRegister(ushort Register, string Name)
{
}

public class GrowattToMqttHandler : IHostedService, IGrowattToMqttHandler
{
    private readonly ILogger<GrowattToMqttHandler> _logger;
    private readonly IMqttConnectionService _mqttConnection;
    private readonly GrowattTopicHelper _topicHelper;
    private readonly List<MqttDiscoveryConfig> _dicoveryConfigs = [];
    private Timer? _registerReaderTimer;
    private readonly Dictionary<string, Func<MqttApplicationMessageReceivedEventArgs, GrowattSocketHandler, Task>> _topicHandlers;
    private readonly GrowattServerListener _serverListener;

    public GrowattToMqttHandler(
        ILogger<GrowattToMqttHandler> logger,
        IMqttConnectionService mqttConnection,
        GrowattTopicHelper topicHelper,
        GrowattServerListener serverListener)
    {
        _serverListener = serverListener;
        _logger = logger;
        _mqttConnection = mqttConnection;
        _topicHelper = topicHelper;

        _topicHandlers = new()
        {
            {_topicHelper.BatteryFirstModeTopic("+"), HandleBatteryFirstCommandAsync },
            {_topicHelper.BatteryFirstChargeSocTopic("+"), HandleBatteryFirstSoCCommandAsync },
            {_topicHelper.ChargeFromAcTopic("+"), HandleChargeFromAcCommandAsync },
            {_topicHelper.ExportLimitTopic("+"), HandleChargeFromAcCommandAsync }
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {hostedService}", GetType().Name);

        _mqttConnection.OnConnectAsync += async (args) => await PublishConfigs();
        _mqttConnection.OnApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;

        await _mqttConnection.SubscribeAsync(_topicHandlers.Select(x => new MqttTopicFilterBuilder().WithTopic(x.Key).Build()).ToArray());

        _registerReaderTimer = new Timer(async (state) => await ReadRegistersAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _logger.LogInformation("Started {hostedService}", GetType().Name);
    }


    private async Task HandleChargeFromAcCommandAsync(MqttApplicationMessageReceivedEventArgs args, GrowattSocketHandler growattSocket)
    {
        var topic = args.ApplicationMessage.Topic;
        var content = args.ApplicationMessage.ConvertPayloadToString();

        bool mode;

        if (content == "false")
        {
            mode = false;
        }
        else if (content == "true")
        {
            mode = true;
        }
        else
        {
            _logger.LogWarning("Received invalid charge from ac mode. {topic}: {content}", topic, content);
            return;
        }

        await growattSocket.SendTelegramAsync(new GrowattInverterCommandTelegram()
        {
            DataloggerId = growattSocket.Info.DataloggerSerial!,
            Register = GrowattInverterRegisters.BatteryFirstAcCharge.Register,
            Value = mode ? (ushort)1 : (ushort)0 // 1 = true, 0 = false
        });
    }
    private async Task HandleBatteryFirstCommandAsync(MqttApplicationMessageReceivedEventArgs args, GrowattSocketHandler growattSocket)
    {
        var topic = args.ApplicationMessage.Topic;
        var content = args.ApplicationMessage.ConvertPayloadToString();
        bool mode;


        if (content == "false")
        {
            mode = false;
        }
        else if (content == "true")
        {
            mode = true;
        }
        else
        {
            _logger.LogWarning("Received invalid battery mode. {topic}: {content}", topic, content);
            return;
        }

        _logger.LogInformation("Setting battery first mode to {value} on {datalogger}", mode, growattSocket.Info.DataloggerSerial);
        await growattSocket.SendTelegramAsync(new GrowattInverterCommandTelegram()
        {
            DataloggerId = growattSocket.Info.DataloggerSerial!,
            Register = GrowattInverterRegisters.BatteryFirst1Enabled.Register,
            Value = mode ? (ushort)1 : (ushort)0 // 1 = true, 0 = false
        });
        await Task.Delay(TimeSpan.FromSeconds(2));
        await growattSocket.SendTelegramAsync(new GrowattInverterCommandTelegram()
        {
            DataloggerId = growattSocket.Info.DataloggerSerial!,
            Register = GrowattInverterRegisters.BatteryFirst1Enabled.Register,
            Value = 0, // 00:00
        });
        await Task.Delay(TimeSpan.FromSeconds(2));
        await growattSocket.SendTelegramAsync(new GrowattInverterCommandTelegram()
        {
            DataloggerId = growattSocket.Info.DataloggerSerial!,
            Register = GrowattInverterRegisters.BatteryFirst1Stop.Register,
            Value = 5947 // 23:59
        });
    }

    private async Task HandleBatteryFirstSoCCommandAsync(MqttApplicationMessageReceivedEventArgs args, GrowattSocketHandler growattSocket)
    {
        var topic = args.ApplicationMessage.Topic;
        var content = args.ApplicationMessage.ConvertPayloadToString();

        if (!ushort.TryParse(content, out var targetSoc) || targetSoc < 0 || targetSoc > 100)
        {
            _logger.LogWarning("Received invalid SoC. {topic}: {content}. Must be a number from 0-100", topic, content);
            return;
        }


        _logger.LogInformation("Setting battery first soc to {value} on {datalogger}", targetSoc, growattSocket.Info.DataloggerSerial);
        await growattSocket.SendTelegramAsync(new GrowattInverterCommandTelegram()
        {
            DataloggerId = growattSocket.Info.DataloggerSerial!,
            Register = GrowattInverterRegisters.BatteryFirstChargeStopSoC.Register,
            Value = targetSoc
        });
    }


    private async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var messageTopic = args.ApplicationMessage.Topic;
        foreach (var (handlerTopic, handler) in _topicHandlers)
        {
            if (MqttTopicFilterComparer.Compare(messageTopic, handlerTopic) == MqttTopicFilterCompareResult.IsMatch)
            {
                if (!_topicHelper.TryGetDatalogger(messageTopic, out var datalogger))
                {
                    _logger.LogWarning("Failed to find datalogger from topic {topic}", messageTopic);
                    continue;
                }


                var loggerSocket = _serverListener.Sockets.Values.FirstOrDefault(x => x.Info.DataloggerSerial?.ToLower() == datalogger?.ToLower());
                if (loggerSocket == null)
                {
                    _logger.LogWarning("Failed to find datalogger socket for {datalogger}", loggerSocket!.Info.DataloggerSerial);
                    continue;
                }

                try
                {
                    await handler(args, loggerSocket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler {handler} failed to handle message", handler.Method.Name);
                }
            }
        }
    }

    public async Task ReadRegistersAsync()
    {
        var interestingRegisters = new GrowattInverterRegister[] {
            new(1044, ""),
            GrowattInverterRegisters.BatteryFirstChargeStopSoC,
            GrowattInverterRegisters.BatteryFirstAcCharge,
            GrowattInverterRegisters.ExportLimitEnableDisable,
            GrowattInverterRegisters.ExportLimitPowerRate,
            GrowattInverterRegisters.LoadFirstStopSocSet };
        _logger.LogInformation("Reading registers");

        try
        {
            foreach (var item in interestingRegisters)
            {
                foreach (var dataLogger in _serverListener.Sockets)
                {
                    if (dataLogger.Value.Info.DataloggerSerial == null)
                    {
                        continue;
                    }

                    var telegram = new GrowattInverterQueryTelegram()
                    {
                        DataloggerId = dataLogger.Value.Info.DataloggerSerial,
                        StartAddress = item.Register,
                        EndAddress = item.Register
                    };
                    await dataLogger.Value.SendTelegramAsync(telegram);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            await Task.Delay(TimeSpan.FromSeconds(10));

            foreach (var dataLogger in _serverListener.Sockets)
            {
                if (dataLogger.Value.Info.DataloggerSerial == null)
                {
                    continue;
                }

                var statusPayload = JsonSerializer.Serialize(new GrowattRegistersStatus
                {
                    Registers = dataLogger.Value.Info.InverterRegisterValues.ToDictionary(x => x.Key, x => new GrowattRegisterStatus
                    {
                        RawValue = x.Value,
                        Name = interestingRegisters.FirstOrDefault(r => r.Register == x.Key)?.Name ?? ""
                    })
                }, GrowattMqttJsonSerializerContext.Default.GrowattRegistersStatus);

                _logger.LogInformation("Publishing registers");
                await _mqttConnection.PublishAsync(
                    new MqttApplicationMessageBuilder()
                    .WithTopic(_topicHelper.GetInverterRegistryStatus(dataLogger.Value.Info.DataloggerSerial))
                    .WithPayload(statusPayload)
                    .Build());
            }

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to query registers");
        }
    }

    public async Task HandleDataTelegramAsync(GrowattSPHData4Telegram data4Telegram)
    {
        await CheckConfigExists(data4Telegram);

        var outputPriority = "UNKNOWN";
        switch (data4Telegram.OutputPriority)
        {
            case 0:
                outputPriority = "LOAD";
                break;
            case 1:
                outputPriority = "BATTERY";
                break;
            case 2:
                outputPriority = "GRID";
                break;
        }

        var statusDto = new GrowattStatusPayload
        {
            Pv1Current = data4Telegram.Pv1current / 10f,
            Pv1Voltage = data4Telegram.Pv1voltage / 10f,
            Pv1Watt = data4Telegram.Pv1watt / 10f,
            Pv2Current = data4Telegram.Pv2current / 10f,
            Pv2Voltage = data4Telegram.Pv2voltage / 10f,
            Pv2Watt = data4Telegram.Pv2watt / 10f,
            Pvpowerin = data4Telegram.Pvpowerin / 10f,
            Pvpowerout = data4Telegram.Pvpowerout / 10f,

            Pvfrequentie = data4Telegram.Pvfrequentie / 100f,
            Pvenergytoday = data4Telegram.Pvenergytoday / 10f,
            Epvtotal = data4Telegram.Epvtotal / 10f,
            Pvtemperature = data4Telegram.Pvtemperature / 10f,

            Pvgridvoltage = data4Telegram.Pvgridvoltage / 10f,
            Pvgridvoltage2 = data4Telegram.Pvgridvoltage2 / 10f,
            Pvgridvoltage3 = data4Telegram.Pvgridvoltage3 / 10f,
            Pvgridcurrent = data4Telegram.Pvgridcurrent / 10f,
            Pvgridcurrent2 = data4Telegram.Pvgridcurrent2 / 10f,
            Pvgridcurrent3 = data4Telegram.Pvgridcurrent3 / 10f,

            // Battery
            StateOfCharge = data4Telegram.SOC,
            Vbat = data4Telegram.Vbat / 10f,
            BatteryTemperature = data4Telegram.Battemp / 10f,
            P1charge1 = data4Telegram.P1charge1 / 10f,
            Pdischarge1 = data4Telegram.Pdischarge1 / 10f,
            Eacharge_today = data4Telegram.EachargeToday / 10f,
            Eacharge_total = data4Telegram.EachargeTotal / 10f,
            Edischarge1_tod = data4Telegram.Edischarge1Today / 10f,
            Edischarge1_total = data4Telegram.Edischarge1Total / 10f,
            Eharge1_tod = data4Telegram.Eharge1_tod / 10f,
            Eharge1_tot = data4Telegram.Eharge1_tot / 10f,

            // Local load
            Elocalload_tod = data4Telegram.Elocalload_tod / 10f,
            Elocalload_tot = data4Telegram.Elocalload_tot / 10f,
            Plocaloadtot = data4Telegram.Plocaloadtot / 10f,

            // Import
            Etouser_tod = data4Telegram.Etouser_tod / 10f,
            Etouser_tot = data4Telegram.Etouser_tot / 10f,
            Pactousertot = data4Telegram.Pactousertot / 10f,

            // Export
            Pactogridtot = data4Telegram.Pactogridtot / 10f,
            Etogrid_tod = data4Telegram.Etogrid_tod / 10f,
            Etogrid_tot = data4Telegram.Etogrid_tot / 10f,

            OutputPriority = outputPriority,

            Timestamp = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds()
        };

        var statusPayload = JsonSerializer.Serialize(statusDto, GrowattMqttJsonSerializerContext.Default.GrowattStatusPayload);

        _mqttConnection.PublishAsync(
            new MqttApplicationMessageBuilder()
            .WithTopic(_topicHelper.GetDataPublishTopic(data4Telegram.Datalogserial))
            .WithPayload(statusPayload)
            .Build())
            .GetAwaiter().GetResult();
    }

    private async Task CheckConfigExists(GrowattSPHData4Telegram data4Telegram)
    {
        if (_dicoveryConfigs.Any(x => x.UniqueId?.Contains(data4Telegram.Datalogserial, StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            return;
        }

        var lastReset = "2021-09-09T00:00:00+00:00";

        var device = new MqttDiscoveryDevice
        {
            Name = "Growatt Shine " + data4Telegram.Datalogserial,
            Identifiers =
                [
                    data4Telegram.Datalogserial
                ]
        };

        _logger.LogInformation("Wasn't able to find any discovery document for {dataLogger}", data4Telegram.Datalogserial);

        // Local Load
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Plocaloadtot), "Local Load Actual", HomeAssistantUnits.POWER_WATT, data4Telegram)
            .SetStateClass(MqttDiscoveryStateClass.Measurement)
            .Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Elocalload_tod), "Local Load Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Elocalload_tot), "Local Load Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);

        // Grid import Load
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pactousertot), "Grid Import Actual", HomeAssistantUnits.POWER_WATT, data4Telegram).SetStateClass(MqttDiscoveryStateClass.Measurement).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Etouser_tod), "Grid Import Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Etouser_tot), "Grid Import Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);

        // Grid export Load
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pactogridtot), "Grid Export Actual", HomeAssistantUnits.POWER_WATT, data4Telegram).SetStateClass(MqttDiscoveryStateClass.Measurement).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Etogrid_tod), "Grid Export Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Etogrid_tot), "Grid Export Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);

        // Solar
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvenergytoday), "Generated Solar Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Epvtotal), "Generated Solar Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pv1Voltage), "Loop 1 Voltage", HomeAssistantUnits.VOLT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pv1Current), "Loop 1 Currrent", HomeAssistantUnits.ELECTRICAL_CURRENT_AMPERE, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pv1Watt), "Loop 1 Watt", HomeAssistantUnits.POWER_WATT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pv2Voltage), "Loop 2 Voltage", HomeAssistantUnits.VOLT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pv2Current), "Loop 2 Currrent", HomeAssistantUnits.ELECTRICAL_CURRENT_AMPERE, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pv2Watt), "Loop 2 Watt", HomeAssistantUnits.POWER_WATT, data4Telegram).Config);

        // Inverter
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvtemperature), "Inverter Temperature", HomeAssistantUnits.TEMP_CELSIUS, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvpowerin), "Inverter Input Watt", HomeAssistantUnits.POWER_WATT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvpowerout), "Inverter Output Watt", HomeAssistantUnits.POWER_WATT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.OutputPriority), "Inverter Output Priority", null, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Eactoday), "Generated Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Eactotal), "Generated Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);

        // Grid Status
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvfrequentie), "Grid Frequency", HomeAssistantUnits.FREQUENCY_HERTZ, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvgridvoltage), "Grid Voltage L1", HomeAssistantUnits.VOLT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvgridvoltage2), "Grid Voltage L2", HomeAssistantUnits.VOLT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvgridvoltage3), "Grid Voltage L3", HomeAssistantUnits.VOLT, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvgridcurrent), "Grid Current L1", HomeAssistantUnits.ELECTRICAL_CURRENT_AMPERE, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvgridcurrent2), "Grid Current L2", HomeAssistantUnits.ELECTRICAL_CURRENT_AMPERE, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pvgridcurrent3), "Grid Current L3", HomeAssistantUnits.ELECTRICAL_CURRENT_AMPERE, data4Telegram).Config);

        // Battery
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.StateOfCharge), "Battery State of Charge", HomeAssistantUnits.PERCENTAGE, data4Telegram).SetStateClass(MqttDiscoveryStateClass.Measurement).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Vbat), "Battery Voltage", HomeAssistantUnits.VOLT, data4Telegram).SetStateClass(MqttDiscoveryStateClass.Measurement).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.BatteryTemperature), "Battery Temperature", HomeAssistantUnits.TEMP_CELSIUS, data4Telegram).SetStateClass(MqttDiscoveryStateClass.Measurement).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Eacharge_today), "Battery AC Charge Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Eacharge_total), "Battery AC Charge Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Eharge1_tod), "Battery Charge Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Eharge1_tot), "Battery Charge Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.P1charge1), "Battery Charge Actual", HomeAssistantUnits.POWER_WATT, data4Telegram).SetStateClass(MqttDiscoveryStateClass.Measurement).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Pdischarge1), "Batter Discharge Actual", HomeAssistantUnits.POWER_WATT, data4Telegram).SetStateClass(MqttDiscoveryStateClass.Measurement).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Edischarge1_tod), "Batter Discharge Today", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).Config);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Edischarge1_total), "Batter Discharge Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);

        // Commands
        _dicoveryConfigs.Add(new MqttButtonDiscoveryConfig()
        {
            Name = "Enable Battery First",
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_enablebatteryfirst",
            Device = device,
            CommandTopic = _topicHelper.BatteryFirstModeTopic(data4Telegram.Datalogserial),
            CommandTemplate = "true"
        });
        _dicoveryConfigs.Add(new MqttButtonDiscoveryConfig()
        {
            Name = "Disable Battery First",
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_disablebatteryfirst",
            Device = device,
            CommandTopic = _topicHelper.BatteryFirstModeTopic(data4Telegram.Datalogserial),
            CommandTemplate = "false"
        });

        _dicoveryConfigs.Add(new MqttButtonDiscoveryConfig()
        {
            Name = "Battery First Enable Charge From AC",
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_enablechargefromac",
            Device = device,
            CommandTopic = _topicHelper.ChargeFromAcTopic(data4Telegram.Datalogserial),
            CommandTemplate = "true"
        });
        _dicoveryConfigs.Add(new MqttButtonDiscoveryConfig()
        {
            Name = "Battery First Disable Charge From AC",
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_disablechargefromac",
            Device = device,
            CommandTopic = _topicHelper.ChargeFromAcTopic(data4Telegram.Datalogserial),
            CommandTemplate = "false"
        });

        _dicoveryConfigs.Add(new MqttNumberDiscoveryConfig()
        {
            Name = "Battery First Stop Charge SoC",
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_batteryfirstsoc",
            Device = device,
            CommandTopic = _topicHelper.BatteryFirstChargeSocTopic(data4Telegram.Datalogserial),
            StateTopic = _topicHelper.GetInverterRegistryStatus(data4Telegram.Datalogserial),
            ValueTemplate = $"{{{{ value_json.['{GrowattInverterRegisters.BatteryFirstChargeStopSoC.Register}'] }}}}",
            Min = 0,
            Max = 100,
            Step = 1,
            UnitOfMeasurement = HomeAssistantUnits.PERCENTAGE.Value
        });

        await PublishConfigs();
    }

    private async Task PublishConfigs()
    {
        _logger.LogInformation("Trying to publish growatt discovery documents");
        if (_dicoveryConfigs == null)
        {
            return;
        }

        foreach (var config in _dicoveryConfigs)
        {
            await _mqttConnection.PublishDiscoveryDocument(config);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {hostedService}", GetType().Name);
        _registerReaderTimer?.Dispose();
        _logger.LogInformation("Stopped {hostedService}", GetType().Name);
        return Task.CompletedTask;
    }
}


public class MqttSensorDiscoveryConfigBuilder
{
    public MqttSensorDiscoveryConfig Config { get; }

    public MqttSensorDiscoveryConfigBuilder(GrowattTopicHelper topicHelper, string propertyName, string displayName, HomeAssistantUnits? unit, GrowattSPHData4Telegram data4Telegram)
    {
        var propertyNaming = GrowattMqttJsonSerializerContext.Default.Options.PropertyNamingPolicy!;

        Config = new MqttSensorDiscoveryConfig
        {
            Name = displayName,
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_" + propertyName.ToLower(),
            Device = new MqttDiscoveryDevice
            {
                Name = "Growatt Shine " + data4Telegram.Datalogserial,
                Identifiers =
                [
                    data4Telegram.Datalogserial
                ]
            },
            Availability =
            [
                new()
                {
                    Topic = topicHelper.GetConnectedTopic(),
                    PayloadAvailable = "2",
                    PayloadNotAvailable = "0"
                }
            ],
            StateTopic = topicHelper.GetDataPublishTopic(data4Telegram.Datalogserial),
            ValueTemplate = $"{{{{ value_json.{propertyNaming.ConvertName(propertyName)}}}}}",
            UnitOfMeasurement = unit?.Value,
        };
    }

    public MqttSensorDiscoveryConfigBuilder SetStateClass(MqttDiscoveryStateClass stateClass)
    {
        Config.StateClass = stateClass;
        return this;
    }

    public MqttSensorDiscoveryConfigBuilder SetDeviceClass(HomeAssistantDeviceClass deviceClass)
    {
        Config.DeviceClass = deviceClass.Value;
        return this;
    }

    public MqttSensorDiscoveryConfigBuilder SetLastReset(string lastReset)
    {
        Config.LastReset = lastReset;
        return this;
    }
}
