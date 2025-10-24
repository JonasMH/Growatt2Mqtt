namespace GrowattShine2Mqtt;

public class GrowattDataloggerInformation {
    public string? DataloggerSerial { get; set; }
    public Dictionary<ushort, byte[]> DataloggerRegisterValues { get; set; } = [];
    public Dictionary<ushort, ushort> InverterRegisterValues { get; set; } = [];
}
