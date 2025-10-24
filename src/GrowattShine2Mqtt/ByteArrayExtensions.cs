namespace GrowattShine2Mqtt;

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
