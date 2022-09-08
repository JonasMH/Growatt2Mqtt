using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GrowattShine2Mqtt;

public class ByteDecoder<T>
{
    private readonly T _instance;
    private readonly ArraySegment<byte> _buffer;

    public T Result => _instance;

    public ByteDecoder(T instance, ArraySegment<byte> buffer)
    {
        _instance = instance;
        _buffer = buffer;
    }

    public ByteDecoder<T> ReadString(Expression<Func<T, string>> memberLamda, int pos, int length)
    {
        var reversed = _buffer.Array[pos..].Take(length).ToArray();
        SetPropertyValue(_instance, memberLamda, Encoding.UTF8.GetString(reversed));
        return this;
    }

    public ByteDecoder<T> ReadInt16(Expression<Func<T, short>> memberLamda, int pos)
    {
        var length = 2;
        var reversed = _buffer.Array[pos..].Take(length).Reverse().ToArray();
        SetPropertyValue(_instance, memberLamda, BitConverter.ToInt16(reversed));
        return this;
    }
    public ByteDecoder<T> ReadByte(Expression<Func<T, byte>> memberLamda, int pos)
    {
        SetPropertyValue(_instance, memberLamda, _buffer.Array[pos]);
        return this;
    }

    public ByteDecoder<T> ReadInt32(Expression<Func<T, int>> memberLamda, int pos)
    {
        var length = 4;
        var reversed = _buffer.Array[pos..].Take(length).Reverse().ToArray();
        SetPropertyValue(_instance, memberLamda, BitConverter.ToInt32(reversed));
        return this;
    }

    public ByteDecoder<T> ReadUInt32(Expression<Func<T, uint>> memberLamda, int pos)
    {
        var length = 4;
        var reversed = _buffer.Array[pos..].Take(length).Reverse().ToArray();
        SetPropertyValue(_instance, memberLamda, BitConverter.ToUInt32(reversed));
        return this;
    }


    private void SetPropertyValue<T, TValue>(T target, Expression<Func<T, TValue>> memberLamda, TValue value)
    {
        var memberSelectorExpression = memberLamda.Body as MemberExpression;
        if (memberSelectorExpression != null)
        {
            var property = memberSelectorExpression.Member as PropertyInfo;
            if (property != null)
            {
                property.SetValue(target, value, null);
            }
        }
    }
}
