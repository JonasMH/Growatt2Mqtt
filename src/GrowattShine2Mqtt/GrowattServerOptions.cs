namespace GrowattShine2Mqtt;

public class GrowattServerOptions
{
    public const string SectionName = "GrowattServer";

    public int Port { get; set; } = 5279;
    public string HostingAddress { get; set; } = "0.0.0.0";
}
