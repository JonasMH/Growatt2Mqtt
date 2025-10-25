namespace GrowattShine2Mqtt;

public class GrowattToMqttOptions
{
    public const string Section = "GrowattToMqtt";

    public List<ushort> InverterRegistersToRead { get; set; } = [];
}
