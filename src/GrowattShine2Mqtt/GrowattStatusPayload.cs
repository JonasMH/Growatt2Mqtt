using System.Text.Json.Serialization;

namespace GrowattShine2Mqtt;

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
