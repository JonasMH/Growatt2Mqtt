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
using System.Reactive;

namespace GrowattShine2Mqtt;

public interface IGrowattToMqttHandler
{
    void NewDataTelegram(GrowattSPHData4Telegram data4Telegram);
}


public class GrowattStatusPayload
{
    public short Pvstatus { get; set; }
    public float Pvpowerin { get; set; }
    public float Pv1Voltage { get; set; }
    public float Pv1Current { get; set; }
    public float Pv1Watt { get; set; }
    public float Pv2Voltage { get; set; }
    public float Pv2Current { get; set; }
    public float Pv2Watt { get; set; }
    public float Pvpowerout { get; set; }
    public float Pvfrequentie { get; set; }
    public float Pvgridvoltage { get; set; }
    public float Pvgridcurrent { get; set; }
    public int Pvgridpower { get; set; }
    public float Pvgridvoltage2 { get; set; }
    public float Pvgridcurrent2 { get; set; }
    public int Pvgridpower2 { get; set; }
    public float Pvgridvoltage3 { get; set; }
    public float Pvgridcurrent3 { get; set; }
    public int Pvgridpower3 { get; set; }
    public float Eactoday { get; set; }
    public float Pvenergytoday { get; set; }
    public float Pdischarge1 { get; set; }
    public float P1charge1 { get; set; }
    public float Vbat { get; set; }
    public short StateOfCharge { get; set; }
    public float Pactousertot { get; set; }
    public float Pactogridtot { get; set; }
    public float BatteryTemperature { get; set; }
    public float Pvtemperature { get; set; }

    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }
    public float Eacharge_today { get; set; }
    public float Eacharge_total { get; set; }
    public float Edischarge1_tod { get; set; }
    public float Edischarge1_total { get; set; }
    public float Elocalload_tod { get; set; }
    public float Elocalload_tot { get; set; }
    public float Plocaloadtot { get; set; }
    public float Etouser_tod { get; internal set; }
    public float Etouser_tot { get; internal set; }
    public float Eactotal { get; internal set; }
    public string OutputPriority { get; internal set; }
    public float Epvtotal { get; internal set; }
    public float Eharge1_tod { get; internal set; }
    public float Eharge1_tot { get; internal set; }
    public float Etogrid_tod { get; internal set; }
    public float Etogrid_tot { get; internal set; }
}

public class GrowattToMqttHandler : IHostedService, IGrowattToMqttHandler
{
    private readonly ILogger<GrowattToMqttHandler> _logger;
    private readonly IMqttConnectionService _mqttConnection;
    private readonly IGrowattTopicHelper _topicHelper;
    private readonly List<ToMqttNet.MqttDiscoveryConfig> _dicoveryConfigs = new();

    public GrowattToMqttHandler(
        ILogger<GrowattToMqttHandler> logger,
        IMqttConnectionService mqttConnection,
        IGrowattTopicHelper topicHelper)
    {
        _logger = logger;
        _mqttConnection = mqttConnection;
        _topicHelper = topicHelper;
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

        var outputPriority = "UNKNOWN";
        switch(data4Telegram.OutputPriority)
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

        var statusPayload = _topicHelper.SerializePayload(new GrowattStatusPayload
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
        });

        _mqttConnection.PublishAsync(
            new MqttApplicationMessageBuilder()
            .WithTopic(_topicHelper.GetDataPublishTopic(data4Telegram.Datalogserial))
            .WithPayload(statusPayload)
            .Build())
            .GetAwaiter().GetResult();
    }

    private void CheckConfigExists(GrowattSPHData4Telegram data4Telegram)
    {
        if (_dicoveryConfigs.Any(x => x.UniqueId?.Contains(data4Telegram.Datalogserial, StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            return;
        }

        var lastReset = "2021-09-09T00:00:00+00:00";

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
        _dicoveryConfigs.Add(new MqttSensorDiscoveryConfigBuilder(_topicHelper, nameof(GrowattStatusPayload.Eacharge_total), "Generated Total", HomeAssistantUnits.ENERGY_KILO_WATT_HOUR, data4Telegram).SetStateClass(MqttDiscoveryStateClass.TotalIncreasing).SetDeviceClass(HomeAssistantDeviceClass.ENERGY).SetLastReset(lastReset).Config);

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


public class MqttSensorDiscoveryConfigBuilder
{
    public MqttSensorDiscoveryConfig Config { get; }

    public MqttSensorDiscoveryConfigBuilder(IGrowattTopicHelper topicHelper, string propertyName, string displayName, HomeAssistantUnits? unit, GrowattSPHData4Telegram data4Telegram)
    {
        Config = new MqttSensorDiscoveryConfig
        {
            Name = "Solar - " + displayName,
            UniqueId = data4Telegram.Datalogserial.ToLower() + "_" + propertyName.ToLower(),
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
                    Topic = topicHelper.GetConnectedTopic(),
                    PayloadAvailable = "2",
                    PayloadNotAvailable = "0"
                }
            },
            StateTopic = topicHelper.GetDataPublishTopic(data4Telegram.Datalogserial),
            ValueTemplate = $"{{{{ value_json.{topicHelper.GetPayloadPropertyName(propertyName)}}}}}",
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
