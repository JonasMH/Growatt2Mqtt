namespace GrowattShine2Mqtt;

public static class ByteArrayExtensions
{
    public static string ToHex(this byte[] input)
    {
        return BitConverter.ToString(input).Replace("-", "");
    }
    public static string ToHex(this short input)
    {
        var buffer = new byte[2];
        BitConverter.TryWriteBytes(buffer, input);
        return BitConverter.ToString(buffer).Replace("-", "");
    }
}
