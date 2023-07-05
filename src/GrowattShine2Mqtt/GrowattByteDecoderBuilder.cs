using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NodaTime;

namespace GrowattShine2Mqtt;

public class GrowattByteDecoder
{
    public static GrowattByteDecoder Instance { get; } = new GrowattByteDecoder();

    public byte[] WriteString(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    public byte[] WriteInt16(short value)
    {
        return BitConverter.GetBytes(value).Reverse().ToArray();
    }

    public byte[] WriteUInt16(ushort value)
    {
        return BitConverter.GetBytes(value).Reverse().ToArray();
    }

    public byte[] WriteInt32(int value)
    {
        return BitConverter.GetBytes(value).Reverse().ToArray();
    }

    public byte[] WriteUInt32(uint value)
    {
        return BitConverter.GetBytes(value).Reverse().ToArray();
    }
    public byte[] WriteGrowattDateTime(LocalDateTime value)
    {
        var buffer = new byte[6];

        buffer[0] = (byte)(value.Year - 2000);
        buffer[1] = (byte)(value.Month);
        buffer[2] = (byte)(value.Day);
        buffer[3] = (byte)(value.Hour);
        buffer[4] = (byte)(value.Minute);
        buffer[5] = (byte)(value.Second);

        return buffer;
    }

    public string ReadString(ArraySegment<byte> buffer, int pos, int length)
    {
        return Encoding.UTF8.GetString(buffer[pos..].Take(length).ToArray());
    }

    public short ReadInt16(ArraySegment<byte> buffer, int pos)
    {
        var length = 2;
        var reversed = buffer[pos..].Take(length).Reverse().ToArray();
        return BitConverter.ToInt16(reversed);
    }

    public ushort ReadUInt16(ArraySegment<byte> buffer, int pos)
    {
        var length = 2;
        var reversed = buffer[pos..].Take(length).Reverse().ToArray();
        return BitConverter.ToUInt16(reversed);
    }

    public byte ReadByte(ArraySegment<byte> buffer, int pos)
    {
        return buffer[pos];
    }

    public int ReadInt32(ArraySegment<byte> buffer, int pos)
    {
        var length = 4;
        var reversed = buffer[pos..].Take(length).Reverse().ToArray();
        return BitConverter.ToInt32(reversed);
    }

    public uint ReadUInt32(ArraySegment<byte> buffer, int pos)
    {
        var length = 4;
        var reversed = buffer[pos..].Take(length).Reverse().ToArray();
        return BitConverter.ToUInt32(reversed);
    }

    public LocalDateTime? ReadGrowattDateTime(ArraySegment<byte> buffer, int pos)
    {
        var year = buffer[pos] + 2000;
        var month = buffer[pos + 1];
        var day = buffer[pos + 2];
        var hour = buffer[pos + 3];
        var minute = buffer[pos + 4];
        var second = buffer[pos + 5];

        if (month == 0)
        {
            return null;
        }

        return new LocalDateTime(year, month, day, hour, minute, second);
    }
}

public class GrowattByteDecoderBuilder<T>
{
    private readonly T _instance;
    private readonly ArraySegment<byte> _buffer;
    private readonly GrowattByteDecoder _decoder = new GrowattByteDecoder();

    public T Result => _instance;

    public GrowattByteDecoderBuilder(T instance, ArraySegment<byte> buffer)
    {
        _instance = instance;
        _buffer = buffer;
    }

    public GrowattByteDecoderBuilder<T> ReadString(Expression<Func<T, string>> memberLamda, int pos, int length)
    {
        SetPropertyValue(_instance, memberLamda, _decoder.ReadString(_buffer, pos, length));
        return this;
    }

    public GrowattByteDecoderBuilder<T> ReadInt16(Expression<Func<T, short>> memberLamda, int pos)
    {
        SetPropertyValue(_instance, memberLamda, _decoder.ReadInt16(_buffer, pos));
        return this;
    }
    
    public GrowattByteDecoderBuilder<T> ReadUInt16(Expression<Func<T, ushort>> memberLamda, int pos)
    {
        SetPropertyValue(_instance, memberLamda, _decoder.ReadUInt16(_buffer, pos));
        return this;
    }

    public GrowattByteDecoderBuilder<T> ReadByte(Expression<Func<T, byte>> memberLamda, int pos)
    {
        SetPropertyValue(_instance, memberLamda, _decoder.ReadByte(_buffer, pos));
        return this;
    }

    public GrowattByteDecoderBuilder<T> ReadInt32(Expression<Func<T, int>> memberLamda, int pos)
    {
        SetPropertyValue(_instance, memberLamda, _decoder.ReadInt32(_buffer, pos));
        return this;
    }

    public GrowattByteDecoderBuilder<T> ReadUInt32(Expression<Func<T, uint>> memberLamda, int pos)
    {
        SetPropertyValue(_instance, memberLamda, _decoder.ReadUInt32(_buffer, pos));
        return this;
    }

    public GrowattByteDecoderBuilder<T> ReadGrowattDateTime(Expression<Func<T, LocalDateTime?>> memberLamda, int pos)
    {
        var result = _decoder.ReadGrowattDateTime(_buffer, pos);

        if (result == null)
        {
            return this;
        }

        SetPropertyValue(_instance, memberLamda, result);
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
