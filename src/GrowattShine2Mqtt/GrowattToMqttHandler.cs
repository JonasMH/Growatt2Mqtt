using System.Xml.Linq;
using System;
using GrowattShine2Mqtt.Telegrams;
using ToMqttNet;
using MQTTnet;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using NodaTime;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GrowattShine2Mqtt;

public interface IGrowattToMqttHandler
{
    void NewDataTelegram(GrowattSPHData4Telegram data4Telegram);
}


public class GrowattStatusPayload
{
    public short Pvstatus { get; set; }
    public int Pvpowerin { get; set; }
    public short Pv1Voltage { get; set; }
    public short Pv1Current { get; set; }
    public int Pv1Watt { get; set; }
    public short Pv2Voltage { get; set; }
    public short Pv2Current { get; set; }
    public int Pv2Watt { get; set; }
    public uint Pvpowerout { get; set; }
    public short Pvfrequentie { get; set; }
    public short Pvgridvoltage { get; set; }
    public short Pvgridcurrent { get; set; }
    public int Pvgridpower { get; set; }
    public short Pvgridvoltage2 { get; set; }
    public short Pvgridcurrent2 { get; set; }
    public int Pvgridpower2 { get; set; }
    public short Pvgridvoltage3 { get; set; }
    public short Pvgridcurrent3 { get; set; }
    public int Pvgridpower3 { get; set; }
    public int Eactoday { get; set; }
    public int Pvenergytoday { get; set; }
    public int Pdischarge1 { get; set; }
    public int P1charge1 { get; set; }
    public short Vbat { get; set; }
    public short StateOfCharge { get; set; }
    public int Pactousertot { get; set; }
    public int Pactogridtot { get; set; }
    public int PLoadTotal { get; set; }
    public int Battemp { get; set; }

    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }
}

public class GrowattToMqttHandler : IHostedService, IGrowattToMqttHandler
{
    private readonly ILogger<GrowattToMqttHandler> _logger;
    private readonly IMqttConnectionService _mqttConnection;
    private readonly IGrowattTopicHelper _topicHelper;
    private readonly List<ToMqttNet.MqttDiscoveryConfig> _dicoveryConfigs = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GrowattToMqttHandler(
        ILogger<GrowattToMqttHandler> logger,
        IMqttConnectionService mqttConnection,
        IGrowattTopicHelper topicHelper)
    {
        _logger = logger;
        _mqttConnection = mqttConnection;
        _topicHelper = topicHelper;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {hostedService}", GetType().Name);

        _mqttConnection.OnConnect += (sender, args) => PublishConfigs();

        _logger.LogInformation("Started {hostedService}", GetType().Name);
        return Task.CompletedTask;
    }

    public void NewDataTelegram(GrowattSPHData4Telegram data4Telegram)
    {
        CheckConfigExists(data4Telegram);

        var statusPayload = JsonSerializer.Serialize(new GrowattStatusPayload
        {
            Pv1Current = data4Telegram.Pv1current,
            Pv1Voltage = data4Telegram.Pv1voltage,
            Pv1Watt = data4Telegram.Pv1watt,
            Pv2Current = data4Telegram.Pv2current,
            Pv2Voltage = data4Telegram.Pv2voltage,
            Pv2Watt = data4Telegram.Pv2watt,
            StateOfCharge = data4Telegram.SOC,
            PLoadTotal = data4Telegram.Plocaloadtot,
            Timestamp = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds()
        }, _jsonSerializerOptions);

        _mqttConnection.PublishAsync(
            new MqttApplicationMessageBuilder()
            .WithTopic(_topicHelper.GetDataPublishTopic(data4Telegram.Datalogserial))
            .WithPayload(statusPayload)
            .Build())
            .RunSynchronously();
    }

    private void CheckConfigExists(GrowattSPHData4Telegram data4Telegram)
    {
        if (_dicoveryConfigs.Any(x => x.UniqueId?.Contains(data4Telegram.Datalogserial, StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            return;
        }

        _logger.LogInformation("Wasn't able to find any discovery document for {dataLogger}", data4Telegram.Datalogserial);
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfig
        {
            Name = "Solar - Battery State of Charge",
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_soc",
            Device = new MqttDiscoveryDevice
            {
                Name = "Growatt Shine " + data4Telegram.Datalogserial,
                Identifiers = new List<string>
                {
                    data4Telegram.Datalogserial
                }
            },
            Availability = new List<MqttDiscoveryAvailablilty>
            {
                new MqttDiscoveryAvailablilty()
                {
                    Topic = _topicHelper.GetConnectedTopic(),
                    PayloadAvailable = "2",
                    PayloadNotAvailable = "0"
                }
            },
            StateTopic = _topicHelper.GetDataPublishTopic(data4Telegram.Datalogserial),
            ValueTemplate = "{{ value_json.stateOfCharge }}"
        });

        PublishConfigs();
    }

    private void PublishConfigs()
    {
        _logger.LogInformation("Trying to publish growatt discovery documents");
        if (_dicoveryConfigs == null)
        {
            return;
        }

        foreach (var config in _dicoveryConfigs)
        {
            _mqttConnection.PublishDiscoveryDocument(config);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {hostedService}", GetType().Name);


        _logger.LogInformation("Stopped {hostedService}", GetType().Name);
        return Task.CompletedTask;
    }
}
