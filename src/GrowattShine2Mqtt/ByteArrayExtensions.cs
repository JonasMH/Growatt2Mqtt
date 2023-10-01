namespace GrowattShine2Mqtt;

public static class ByteArrayExtensions
{
    public static string ToHex(this ArraySegment<byte> input)
    {
        return BitConverter.ToString(input.Array!, input.Offset, input.Count).Replace("-", "");
    }
    public static string ToHex(this byte[] input)
    {
        return BitConverter.ToString(input).Replace("-", "");
    }
    public static string ToHex(this short input)
    {
        return BitConverter.ToString(BitConverter.GetBytes(input).Reverse().ToArray()).Replace("-", "");
    }
    public static string ToHex(this ushort input)
    {
        return BitConverter.ToString(BitConverter.GetBytes(input).Reverse().ToArray()).Replace("-", "");
    }
}

public static class StringExtensions
{

    public static byte[] ParseHex(this string input)
    {
        return Enumerable.Range(0, input.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(input.Substring(x, 2), 16))
                         .ToArray();
    }
}
