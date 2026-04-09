using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using Npgsql;
using NpgsqlTypes;

namespace StringThing.Npgsql;

[Union]
public readonly record struct PostgresValue : IUnion
{
    public enum Tag : byte
    {
        None = 0,
        Boolean,
        Int16,
        Int32,
        Int64,
        Single,
        Double,
        Decimal,
        Char,
        Guid,
        DateTime,
        DateTimeOffset,
        DateOnly,
        TimeOnly,
        TimeSpan,
        NpgsqlInterval,
        NpgsqlPoint,
        String,
        ByteArray,
        BitArray,
        IPAddress,
        PhysicalAddress,
        NpgsqlInet,
        NpgsqlCidr,
        NpgsqlBox,
        NpgsqlLSeg,
        NpgsqlCircle,
        NpgsqlLine,
        NpgsqlPath,
        NpgsqlPolygon,
        NpgsqlTsVector,
        NpgsqlTsQuery,
        BigInteger,
        RangeInt32,
        RangeInt64,
        RangeDecimal,
        RangeDateTime,
        RangeDateTimeOffset,
        RangeDateOnly,
        BooleanArray,
        Int16Array,
        Int32Array,
        Int64Array,
        SingleArray,
        DoubleArray,
        DecimalArray,
        StringArray,
        GuidArray,
        DateTimeArray,
        DateTimeOffsetArray,
        DateOnlyArray,
        TimeOnlyArray,
        TimeSpanArray,
        IPAddressArray,
        NpgsqlIntervalArray,
        NpgsqlPointArray,
        PhysicalAddressArray,
        NpgsqlInetArray,
        NpgsqlCidrArray,
        NpgsqlBoxArray,
        NpgsqlLSegArray,
        NpgsqlCircleArray,
        NpgsqlLineArray,
        NpgsqlPathArray,
        NpgsqlPolygonArray,
        NpgsqlTsVectorArray,
        NpgsqlTsQueryArray,
        BigIntegerArray,
        CharArray,
        Jsonb,
        JsonbArray,
        DbNull,
    }

    private readonly Tag _tag;
    private readonly Int128 _valueSlot;
    private readonly object? _referenceSlot;

    public PostgresValue(bool value)
    {
        _tag = Tag.Boolean;
        _valueSlot = value ? 1 : 0;
        _referenceSlot = null;
    }

    public PostgresValue(short value)
    {
        _tag = Tag.Int16;
        _valueSlot = value;
        _referenceSlot = null;
    }

    public PostgresValue(int value)
    {
        _tag = Tag.Int32;
        _valueSlot = value;
        _referenceSlot = null;
    }

    public PostgresValue(long value)
    {
        _tag = Tag.Int64;
        _valueSlot = value;
        _referenceSlot = null;
    }

    public PostgresValue(float value)
    {
        _tag = Tag.Single;
        _valueSlot = BitConverter.SingleToInt32Bits(value);
        _referenceSlot = null;
    }

    public PostgresValue(double value)
    {
        _tag = Tag.Double;
        _valueSlot = BitConverter.DoubleToInt64Bits(value);
        _referenceSlot = null;
    }

    public PostgresValue(decimal value)
    {
        _tag = Tag.Decimal;
        _valueSlot = Unsafe.BitCast<decimal, Int128>(value);
        _referenceSlot = null;
    }

    public PostgresValue(char value)
    {
        _tag = Tag.Char;
        _valueSlot = value;
        _referenceSlot = null;
    }

    public PostgresValue(Guid value)
    {
        _tag = Tag.Guid;
        _valueSlot = Unsafe.BitCast<Guid, Int128>(value);
        _referenceSlot = null;
    }

    public PostgresValue(DateTime value)
    {
        _tag = Tag.DateTime;
        _valueSlot = Unsafe.BitCast<DateTime, long>(value);
        _referenceSlot = null;
    }

    public PostgresValue(DateTimeOffset value)
    {
        _tag = Tag.DateTimeOffset;
        _valueSlot = Unsafe.BitCast<DateTimeOffset, Int128>(value);
        _referenceSlot = null;
    }

    public PostgresValue(DateOnly value)
    {
        _tag = Tag.DateOnly;
        _valueSlot = value.DayNumber;
        _referenceSlot = null;
    }

    public PostgresValue(TimeOnly value)
    {
        _tag = Tag.TimeOnly;
        _valueSlot = value.Ticks;
        _referenceSlot = null;
    }

    public PostgresValue(TimeSpan value)
    {
        _tag = Tag.TimeSpan;
        _valueSlot = value.Ticks;
        _referenceSlot = null;
    }

    public PostgresValue(NpgsqlInterval value)
    {
        _tag = Tag.NpgsqlInterval;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlPoint value)
    {
        _tag = Tag.NpgsqlPoint;
        _valueSlot = Unsafe.BitCast<NpgsqlPoint, Int128>(value);
        _referenceSlot = null;
    }

    public PostgresValue(string value)
    {
        _tag = Tag.String;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(byte[] value)
    {
        _tag = Tag.ByteArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(BitArray value)
    {
        _tag = Tag.BitArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(IPAddress value)
    {
        _tag = Tag.IPAddress;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(PhysicalAddress value)
    {
        _tag = Tag.PhysicalAddress;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlInet value)
    {
        _tag = Tag.NpgsqlInet;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlCidr value)
    {
        _tag = Tag.NpgsqlCidr;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlBox value)
    {
        _tag = Tag.NpgsqlBox;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlLSeg value)
    {
        _tag = Tag.NpgsqlLSeg;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlCircle value)
    {
        _tag = Tag.NpgsqlCircle;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlLine value)
    {
        _tag = Tag.NpgsqlLine;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlPath value)
    {
        _tag = Tag.NpgsqlPath;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlPolygon value)
    {
        _tag = Tag.NpgsqlPolygon;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlTsVector value)
    {
        _tag = Tag.NpgsqlTsVector;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlTsQuery value)
    {
        _tag = Tag.NpgsqlTsQuery;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(BigInteger value)
    {
        _tag = Tag.BigInteger;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlRange<int> value)
    {
        _tag = Tag.RangeInt32;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlRange<long> value)
    {
        _tag = Tag.RangeInt64;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlRange<decimal> value)
    {
        _tag = Tag.RangeDecimal;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlRange<DateTime> value)
    {
        _tag = Tag.RangeDateTime;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlRange<DateTimeOffset> value)
    {
        _tag = Tag.RangeDateTimeOffset;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlRange<DateOnly> value)
    {
        _tag = Tag.RangeDateOnly;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(bool[] value)
    {
        _tag = Tag.BooleanArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(short[] value)
    {
        _tag = Tag.Int16Array;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(int[] value)
    {
        _tag = Tag.Int32Array;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(long[] value)
    {
        _tag = Tag.Int64Array;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(float[] value)
    {
        _tag = Tag.SingleArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(double[] value)
    {
        _tag = Tag.DoubleArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(decimal[] value)
    {
        _tag = Tag.DecimalArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(string[] value)
    {
        _tag = Tag.StringArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(Guid[] value)
    {
        _tag = Tag.GuidArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(DateTime[] value)
    {
        _tag = Tag.DateTimeArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(DateTimeOffset[] value)
    {
        _tag = Tag.DateTimeOffsetArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(DateOnly[] value)
    {
        _tag = Tag.DateOnlyArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(TimeOnly[] value)
    {
        _tag = Tag.TimeOnlyArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(TimeSpan[] value)
    {
        _tag = Tag.TimeSpanArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(IPAddress[] value)
    {
        _tag = Tag.IPAddressArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlInterval[] value)
    {
        _tag = Tag.NpgsqlIntervalArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlPoint[] value)
    {
        _tag = Tag.NpgsqlPointArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(PhysicalAddress[] value)
    {
        _tag = Tag.PhysicalAddressArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlInet[] value)
    {
        _tag = Tag.NpgsqlInetArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlCidr[] value)
    {
        _tag = Tag.NpgsqlCidrArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlBox[] value)
    {
        _tag = Tag.NpgsqlBoxArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlLSeg[] value)
    {
        _tag = Tag.NpgsqlLSegArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlCircle[] value)
    {
        _tag = Tag.NpgsqlCircleArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlLine[] value)
    {
        _tag = Tag.NpgsqlLineArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlPath[] value)
    {
        _tag = Tag.NpgsqlPathArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlPolygon[] value)
    {
        _tag = Tag.NpgsqlPolygonArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlTsVector[] value)
    {
        _tag = Tag.NpgsqlTsVectorArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(NpgsqlTsQuery[] value)
    {
        _tag = Tag.NpgsqlTsQueryArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(BigInteger[] value)
    {
        _tag = Tag.BigIntegerArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(char[] value)
    {
        _tag = Tag.CharArray;
        _valueSlot = default;
        _referenceSlot = value;
    }

    public PostgresValue(IPostgresJson value)
    {
        _tag = Tag.Jsonb;
        _valueSlot = default;
        _referenceSlot = value.ToJson();
    }

    public PostgresValue(IPostgresJson[] values)
    {
        _tag = Tag.JsonbArray;
        _valueSlot = default;
        var jsonStrings = new string[values.Length];
        for (var i = 0; i < values.Length; i++)
            jsonStrings[i] = values[i].ToJson();
        _referenceSlot = jsonStrings;
    }

    private PostgresValue(Tag tag)
    {
        _tag = tag;
        _valueSlot = default;
        _referenceSlot = null;
    }

    public static PostgresValue Null => new(Tag.DbNull);

    public bool HasValue => _tag != Tag.None;

    // --- Non-boxing access pattern ---

    public bool TryGetValue(out bool value)
    {
        if (_tag == Tag.Boolean)
        {
            value = (int)_valueSlot != 0;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out short value)
    {
        if (_tag == Tag.Int16)
        {
            value = (short)(int)_valueSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out int value)
    {
        if (_tag == Tag.Int32)
        {
            value = (int)_valueSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out long value)
    {
        if (_tag == Tag.Int64)
        {
            value = (long)_valueSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out float value)
    {
        if (_tag == Tag.Single)
        {
            value = BitConverter.Int32BitsToSingle((int)_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out double value)
    {
        if (_tag == Tag.Double)
        {
            value = BitConverter.Int64BitsToDouble((long)_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out decimal value)
    {
        if (_tag == Tag.Decimal)
        {
            value = Unsafe.BitCast<Int128, decimal>(_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out char value)
    {
        if (_tag == Tag.Char)
        {
            value = (char)(int)_valueSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out Guid value)
    {
        if (_tag == Tag.Guid)
        {
            value = Unsafe.BitCast<Int128, Guid>(_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out DateTime value)
    {
        if (_tag == Tag.DateTime)
        {
            value = Unsafe.BitCast<long, DateTime>((long)_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out DateTimeOffset value)
    {
        if (_tag == Tag.DateTimeOffset)
        {
            value = Unsafe.BitCast<Int128, DateTimeOffset>(_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out DateOnly value)
    {
        if (_tag == Tag.DateOnly)
        {
            value = DateOnly.FromDayNumber((int)_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out TimeOnly value)
    {
        if (_tag == Tag.TimeOnly)
        {
            value = new TimeOnly((long)_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out TimeSpan value)
    {
        if (_tag == Tag.TimeSpan)
        {
            value = new TimeSpan((long)_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlPoint value)
    {
        if (_tag == Tag.NpgsqlPoint)
        {
            value = Unsafe.BitCast<Int128, NpgsqlPoint>(_valueSlot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out string? value)
    {
        if (_tag == Tag.String)
        {
            value = (string?)_referenceSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out byte[]? value)
    {
        if (_tag == Tag.ByteArray)
        {
            value = (byte[]?)_referenceSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out BitArray? value)
    {
        if (_tag == Tag.BitArray)
        {
            value = (BitArray?)_referenceSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out IPAddress? value)
    {
        if (_tag == Tag.IPAddress)
        {
            value = (IPAddress?)_referenceSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out PhysicalAddress? value)
    {
        if (_tag == Tag.PhysicalAddress)
        {
            value = (PhysicalAddress?)_referenceSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlInet value)
    {
        if (_tag == Tag.NpgsqlInet)
        {
            value = (NpgsqlInet)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlCidr value)
    {
        if (_tag == Tag.NpgsqlCidr)
        {
            value = (NpgsqlCidr)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlBox value)
    {
        if (_tag == Tag.NpgsqlBox)
        {
            value = (NpgsqlBox)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlLSeg value)
    {
        if (_tag == Tag.NpgsqlLSeg)
        {
            value = (NpgsqlLSeg)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlCircle value)
    {
        if (_tag == Tag.NpgsqlCircle)
        {
            value = (NpgsqlCircle)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlLine value)
    {
        if (_tag == Tag.NpgsqlLine)
        {
            value = (NpgsqlLine)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlPath value)
    {
        if (_tag == Tag.NpgsqlPath)
        {
            value = (NpgsqlPath)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlPolygon value)
    {
        if (_tag == Tag.NpgsqlPolygon)
        {
            value = (NpgsqlPolygon)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlTsVector? value)
    {
        if (_tag == Tag.NpgsqlTsVector)
        {
            value = (NpgsqlTsVector?)_referenceSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlTsQuery? value)
    {
        if (_tag == Tag.NpgsqlTsQuery)
        {
            value = (NpgsqlTsQuery?)_referenceSlot;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out BigInteger value)
    {
        if (_tag == Tag.BigInteger)
        {
            value = (BigInteger)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlRange<int> value)
    {
        if (_tag == Tag.RangeInt32)
        {
            value = (NpgsqlRange<int>)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlRange<long> value)
    {
        if (_tag == Tag.RangeInt64)
        {
            value = (NpgsqlRange<long>)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlRange<decimal> value)
    {
        if (_tag == Tag.RangeDecimal)
        {
            value = (NpgsqlRange<decimal>)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlRange<DateTime> value)
    {
        if (_tag == Tag.RangeDateTime)
        {
            value = (NpgsqlRange<DateTime>)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlRange<DateTimeOffset> value)
    {
        if (_tag == Tag.RangeDateTimeOffset)
        {
            value = (NpgsqlRange<DateTimeOffset>)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out NpgsqlRange<DateOnly> value)
    {
        if (_tag == Tag.RangeDateOnly)
        {
            value = (NpgsqlRange<DateOnly>)_referenceSlot!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue(out bool[] value)
    {
        if (_tag == Tag.BooleanArray) { value = (bool[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out short[] value)
    {
        if (_tag == Tag.Int16Array) { value = (short[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out int[] value)
    {
        if (_tag == Tag.Int32Array) { value = (int[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out long[] value)
    {
        if (_tag == Tag.Int64Array) { value = (long[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out float[] value)
    {
        if (_tag == Tag.SingleArray) { value = (float[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out double[] value)
    {
        if (_tag == Tag.DoubleArray) { value = (double[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out decimal[] value)
    {
        if (_tag == Tag.DecimalArray) { value = (decimal[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out string[] value)
    {
        if (_tag == Tag.StringArray) { value = (string[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out Guid[] value)
    {
        if (_tag == Tag.GuidArray) { value = (Guid[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out DateTime[] value)
    {
        if (_tag == Tag.DateTimeArray) { value = (DateTime[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out DateTimeOffset[] value)
    {
        if (_tag == Tag.DateTimeOffsetArray) { value = (DateTimeOffset[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out DateOnly[] value)
    {
        if (_tag == Tag.DateOnlyArray) { value = (DateOnly[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out TimeOnly[] value)
    {
        if (_tag == Tag.TimeOnlyArray) { value = (TimeOnly[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out TimeSpan[] value)
    {
        if (_tag == Tag.TimeSpanArray) { value = (TimeSpan[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out IPAddress[] value)
    {
        if (_tag == Tag.IPAddressArray) { value = (IPAddress[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlInterval[] value)
    {
        if (_tag == Tag.NpgsqlIntervalArray) { value = (NpgsqlInterval[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlPoint[] value)
    {
        if (_tag == Tag.NpgsqlPointArray) { value = (NpgsqlPoint[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out PhysicalAddress[] value)
    {
        if (_tag == Tag.PhysicalAddressArray) { value = (PhysicalAddress[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlInet[] value)
    {
        if (_tag == Tag.NpgsqlInetArray) { value = (NpgsqlInet[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlCidr[] value)
    {
        if (_tag == Tag.NpgsqlCidrArray) { value = (NpgsqlCidr[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlBox[] value)
    {
        if (_tag == Tag.NpgsqlBoxArray) { value = (NpgsqlBox[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlLSeg[] value)
    {
        if (_tag == Tag.NpgsqlLSegArray) { value = (NpgsqlLSeg[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlCircle[] value)
    {
        if (_tag == Tag.NpgsqlCircleArray) { value = (NpgsqlCircle[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlLine[] value)
    {
        if (_tag == Tag.NpgsqlLineArray) { value = (NpgsqlLine[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlPath[] value)
    {
        if (_tag == Tag.NpgsqlPathArray) { value = (NpgsqlPath[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlPolygon[] value)
    {
        if (_tag == Tag.NpgsqlPolygonArray) { value = (NpgsqlPolygon[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlTsVector[] value)
    {
        if (_tag == Tag.NpgsqlTsVectorArray) { value = (NpgsqlTsVector[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out NpgsqlTsQuery[] value)
    {
        if (_tag == Tag.NpgsqlTsQueryArray) { value = (NpgsqlTsQuery[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out BigInteger[] value)
    {
        if (_tag == Tag.BigIntegerArray) { value = (BigInteger[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public bool TryGetValue(out char[] value)
    {
        if (_tag == Tag.CharArray) { value = (char[])_referenceSlot!; return true; }
        value = default!; return false;
    }

    public object? Value => _tag switch
    {
        Tag.Boolean => (int)_valueSlot != 0,
        Tag.Int16 => (short)(int)_valueSlot,
        Tag.Int32 => (int)_valueSlot,
        Tag.Int64 => (long)_valueSlot,
        Tag.Single => BitConverter.Int32BitsToSingle((int)_valueSlot),
        Tag.Double => BitConverter.Int64BitsToDouble((long)_valueSlot),
        Tag.Decimal => Unsafe.BitCast<Int128, decimal>(_valueSlot),
        Tag.Char => (char)(int)_valueSlot,
        Tag.Guid => Unsafe.BitCast<Int128, Guid>(_valueSlot),
        Tag.DateTime => Unsafe.BitCast<long, DateTime>((long)_valueSlot),
        Tag.DateTimeOffset => Unsafe.BitCast<Int128, DateTimeOffset>(_valueSlot),
        Tag.DateOnly => DateOnly.FromDayNumber((int)_valueSlot),
        Tag.TimeOnly => new TimeOnly((long)_valueSlot),
        Tag.TimeSpan => new TimeSpan((long)_valueSlot),
        Tag.NpgsqlPoint => Unsafe.BitCast<Int128, NpgsqlPoint>(_valueSlot),
        Tag.DbNull => DBNull.Value,
        _ => _referenceSlot,
    };

    public NpgsqlParameter ToNpgsqlParameter() => _tag switch
    {
        Tag.Boolean => new NpgsqlParameter<bool>("", (int)_valueSlot != 0),
        Tag.Int16 => new NpgsqlParameter<short>("", (short)(int)_valueSlot),
        Tag.Int32 => new NpgsqlParameter<int>("", (int)_valueSlot),
        Tag.Int64 => new NpgsqlParameter<long>("", (long)_valueSlot),
        Tag.Single => new NpgsqlParameter<float>("", BitConverter.Int32BitsToSingle((int)_valueSlot)),
        Tag.Double => new NpgsqlParameter<double>("", BitConverter.Int64BitsToDouble((long)_valueSlot)),
        Tag.Decimal => new NpgsqlParameter<decimal>("", Unsafe.BitCast<Int128, decimal>(_valueSlot)),
        Tag.Char => new NpgsqlParameter<char>("", (char)(int)_valueSlot),
        Tag.Guid => new NpgsqlParameter<Guid>("", Unsafe.BitCast<Int128, Guid>(_valueSlot)),
        Tag.DateTime => new NpgsqlParameter<DateTime>("", Unsafe.BitCast<long, DateTime>((long)_valueSlot)),
        Tag.DateTimeOffset => new NpgsqlParameter<DateTimeOffset>("", Unsafe.BitCast<Int128, DateTimeOffset>(_valueSlot)),
        Tag.DateOnly => new NpgsqlParameter<DateOnly>("", DateOnly.FromDayNumber((int)_valueSlot)),
        Tag.TimeOnly => new NpgsqlParameter<TimeOnly>("", new TimeOnly((long)_valueSlot)),
        Tag.TimeSpan => new NpgsqlParameter<TimeSpan>("", new TimeSpan((long)_valueSlot)),
        Tag.NpgsqlInterval => new NpgsqlParameter<NpgsqlInterval>("", (NpgsqlInterval)_referenceSlot!),
        Tag.NpgsqlPoint => new NpgsqlParameter<NpgsqlPoint>("", Unsafe.BitCast<Int128, NpgsqlPoint>(_valueSlot)),
        Tag.String => new NpgsqlParameter<string>("", (string)_referenceSlot!),
        Tag.ByteArray => new NpgsqlParameter<byte[]>("", (byte[])_referenceSlot!),
        Tag.BitArray => new NpgsqlParameter<BitArray>("", (BitArray)_referenceSlot!),
        Tag.IPAddress => new NpgsqlParameter<IPAddress>("", (IPAddress)_referenceSlot!),
        Tag.PhysicalAddress => new NpgsqlParameter<PhysicalAddress>("", (PhysicalAddress)_referenceSlot!),
        Tag.NpgsqlInet => new NpgsqlParameter<NpgsqlInet>("", (NpgsqlInet)_referenceSlot!),
        Tag.NpgsqlCidr => new NpgsqlParameter<NpgsqlCidr>("", (NpgsqlCidr)_referenceSlot!),
        Tag.NpgsqlBox => new NpgsqlParameter<NpgsqlBox>("", (NpgsqlBox)_referenceSlot!),
        Tag.NpgsqlLSeg => new NpgsqlParameter<NpgsqlLSeg>("", (NpgsqlLSeg)_referenceSlot!),
        Tag.NpgsqlCircle => new NpgsqlParameter<NpgsqlCircle>("", (NpgsqlCircle)_referenceSlot!),
        Tag.NpgsqlLine => new NpgsqlParameter<NpgsqlLine>("", (NpgsqlLine)_referenceSlot!),
        Tag.NpgsqlPath => new NpgsqlParameter<NpgsqlPath>("", (NpgsqlPath)_referenceSlot!),
        Tag.NpgsqlPolygon => new NpgsqlParameter<NpgsqlPolygon>("", (NpgsqlPolygon)_referenceSlot!),
        Tag.NpgsqlTsVector => new NpgsqlParameter<NpgsqlTsVector>("", (NpgsqlTsVector)_referenceSlot!),
        Tag.NpgsqlTsQuery => new NpgsqlParameter<NpgsqlTsQuery>("", (NpgsqlTsQuery)_referenceSlot!),
        Tag.BigInteger => new NpgsqlParameter<BigInteger>("", (BigInteger)_referenceSlot!),
        Tag.RangeInt32 => new NpgsqlParameter<NpgsqlRange<int>>("", (NpgsqlRange<int>)_referenceSlot!),
        Tag.RangeInt64 => new NpgsqlParameter<NpgsqlRange<long>>("", (NpgsqlRange<long>)_referenceSlot!),
        Tag.RangeDecimal => new NpgsqlParameter<NpgsqlRange<decimal>>("", (NpgsqlRange<decimal>)_referenceSlot!),
        Tag.RangeDateTime => new NpgsqlParameter<NpgsqlRange<DateTime>>("", (NpgsqlRange<DateTime>)_referenceSlot!),
        Tag.RangeDateTimeOffset => new NpgsqlParameter<NpgsqlRange<DateTimeOffset>>("", (NpgsqlRange<DateTimeOffset>)_referenceSlot!),
        Tag.RangeDateOnly => new NpgsqlParameter<NpgsqlRange<DateOnly>>("", (NpgsqlRange<DateOnly>)_referenceSlot!),
        Tag.BooleanArray => new NpgsqlParameter<bool[]>("", (bool[])_referenceSlot!),
        Tag.Int16Array => new NpgsqlParameter<short[]>("", (short[])_referenceSlot!),
        Tag.Int32Array => new NpgsqlParameter<int[]>("", (int[])_referenceSlot!),
        Tag.Int64Array => new NpgsqlParameter<long[]>("", (long[])_referenceSlot!),
        Tag.SingleArray => new NpgsqlParameter<float[]>("", (float[])_referenceSlot!),
        Tag.DoubleArray => new NpgsqlParameter<double[]>("", (double[])_referenceSlot!),
        Tag.DecimalArray => new NpgsqlParameter<decimal[]>("", (decimal[])_referenceSlot!),
        Tag.StringArray => new NpgsqlParameter<string[]>("", (string[])_referenceSlot!),
        Tag.GuidArray => new NpgsqlParameter<Guid[]>("", (Guid[])_referenceSlot!),
        Tag.DateTimeArray => new NpgsqlParameter<DateTime[]>("", (DateTime[])_referenceSlot!),
        Tag.DateTimeOffsetArray => new NpgsqlParameter<DateTimeOffset[]>("", (DateTimeOffset[])_referenceSlot!),
        Tag.DateOnlyArray => new NpgsqlParameter<DateOnly[]>("", (DateOnly[])_referenceSlot!),
        Tag.TimeOnlyArray => new NpgsqlParameter<TimeOnly[]>("", (TimeOnly[])_referenceSlot!),
        Tag.TimeSpanArray => new NpgsqlParameter<TimeSpan[]>("", (TimeSpan[])_referenceSlot!),
        Tag.IPAddressArray => new NpgsqlParameter<IPAddress[]>("", (IPAddress[])_referenceSlot!),
        Tag.NpgsqlIntervalArray => new NpgsqlParameter<NpgsqlInterval[]>("", (NpgsqlInterval[])_referenceSlot!),
        Tag.NpgsqlPointArray => new NpgsqlParameter<NpgsqlPoint[]>("", (NpgsqlPoint[])_referenceSlot!),
        Tag.PhysicalAddressArray => new NpgsqlParameter<PhysicalAddress[]>("", (PhysicalAddress[])_referenceSlot!),
        Tag.NpgsqlInetArray => new NpgsqlParameter<NpgsqlInet[]>("", (NpgsqlInet[])_referenceSlot!),
        Tag.NpgsqlCidrArray => new NpgsqlParameter<NpgsqlCidr[]>("", (NpgsqlCidr[])_referenceSlot!),
        Tag.NpgsqlBoxArray => new NpgsqlParameter<NpgsqlBox[]>("", (NpgsqlBox[])_referenceSlot!),
        Tag.NpgsqlLSegArray => new NpgsqlParameter<NpgsqlLSeg[]>("", (NpgsqlLSeg[])_referenceSlot!),
        Tag.NpgsqlCircleArray => new NpgsqlParameter<NpgsqlCircle[]>("", (NpgsqlCircle[])_referenceSlot!),
        Tag.NpgsqlLineArray => new NpgsqlParameter<NpgsqlLine[]>("", (NpgsqlLine[])_referenceSlot!),
        Tag.NpgsqlPathArray => new NpgsqlParameter<NpgsqlPath[]>("", (NpgsqlPath[])_referenceSlot!),
        Tag.NpgsqlPolygonArray => new NpgsqlParameter<NpgsqlPolygon[]>("", (NpgsqlPolygon[])_referenceSlot!),
        Tag.NpgsqlTsVectorArray => new NpgsqlParameter<NpgsqlTsVector[]>("", (NpgsqlTsVector[])_referenceSlot!),
        Tag.NpgsqlTsQueryArray => new NpgsqlParameter<NpgsqlTsQuery[]>("", (NpgsqlTsQuery[])_referenceSlot!),
        Tag.BigIntegerArray => new NpgsqlParameter<BigInteger[]>("", (BigInteger[])_referenceSlot!),
        Tag.CharArray => new NpgsqlParameter<char[]>("", (char[])_referenceSlot!),
        Tag.Jsonb => new NpgsqlParameter { Value = (string)_referenceSlot!, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb },
        Tag.JsonbArray => new NpgsqlParameter { Value = (string[])_referenceSlot!, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb | NpgsqlTypes.NpgsqlDbType.Array },
        Tag.DbNull => new NpgsqlParameter { Value = DBNull.Value },
        _ => throw new InvalidOperationException($"PostgresValue has no value (tag: {_tag})"),
    };

    // --- Implicit conversions ---

    public static implicit operator PostgresValue(bool value) => new(value);
    public static implicit operator PostgresValue(short value) => new(value);
    public static implicit operator PostgresValue(int value) => new(value);
    public static implicit operator PostgresValue(long value) => new(value);
    public static implicit operator PostgresValue(float value) => new(value);
    public static implicit operator PostgresValue(double value) => new(value);
    public static implicit operator PostgresValue(decimal value) => new(value);
    public static implicit operator PostgresValue(char value) => new(value);
    public static implicit operator PostgresValue(Guid value) => new(value);
    public static implicit operator PostgresValue(DateTime value) => new(value);
    public static implicit operator PostgresValue(DateTimeOffset value) => new(value);
    public static implicit operator PostgresValue(DateOnly value) => new(value);
    public static implicit operator PostgresValue(TimeOnly value) => new(value);
    public static implicit operator PostgresValue(TimeSpan value) => new(value);
    public static implicit operator PostgresValue(NpgsqlInterval value) => new(value);
    public static implicit operator PostgresValue(NpgsqlPoint value) => new(value);
    public static implicit operator PostgresValue(string value) => new(value);
    public static implicit operator PostgresValue(byte[] value) => new(value);
    public static implicit operator PostgresValue(BitArray value) => new(value);
    public static implicit operator PostgresValue(IPAddress value) => new(value);
    public static implicit operator PostgresValue(PhysicalAddress value) => new(value);
    public static implicit operator PostgresValue(NpgsqlInet value) => new(value);
    public static implicit operator PostgresValue(NpgsqlCidr value) => new(value);
    public static implicit operator PostgresValue(NpgsqlBox value) => new(value);
    public static implicit operator PostgresValue(NpgsqlLSeg value) => new(value);
    public static implicit operator PostgresValue(NpgsqlCircle value) => new(value);
    public static implicit operator PostgresValue(NpgsqlLine value) => new(value);
    public static implicit operator PostgresValue(NpgsqlPath value) => new(value);
    public static implicit operator PostgresValue(NpgsqlPolygon value) => new(value);
    public static implicit operator PostgresValue(NpgsqlTsVector value) => new(value);
    public static implicit operator PostgresValue(NpgsqlTsQuery value) => new(value);
    public static implicit operator PostgresValue(BigInteger value) => new(value);
    public static implicit operator PostgresValue(NpgsqlRange<int> value) => new(value);
    public static implicit operator PostgresValue(NpgsqlRange<long> value) => new(value);
    public static implicit operator PostgresValue(NpgsqlRange<decimal> value) => new(value);
    public static implicit operator PostgresValue(NpgsqlRange<DateTime> value) => new(value);
    public static implicit operator PostgresValue(NpgsqlRange<DateTimeOffset> value) => new(value);
    public static implicit operator PostgresValue(NpgsqlRange<DateOnly> value) => new(value);
    public static implicit operator PostgresValue(bool[] value) => new(value);
    public static implicit operator PostgresValue(short[] value) => new(value);
    public static implicit operator PostgresValue(int[] value) => new(value);
    public static implicit operator PostgresValue(long[] value) => new(value);
    public static implicit operator PostgresValue(float[] value) => new(value);
    public static implicit operator PostgresValue(double[] value) => new(value);
    public static implicit operator PostgresValue(decimal[] value) => new(value);
    public static implicit operator PostgresValue(string[] value) => new(value);
    public static implicit operator PostgresValue(Guid[] value) => new(value);
    public static implicit operator PostgresValue(DateTime[] value) => new(value);
    public static implicit operator PostgresValue(DateTimeOffset[] value) => new(value);
    public static implicit operator PostgresValue(DateOnly[] value) => new(value);
    public static implicit operator PostgresValue(TimeOnly[] value) => new(value);
    public static implicit operator PostgresValue(TimeSpan[] value) => new(value);
    public static implicit operator PostgresValue(IPAddress[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlInterval[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlPoint[] value) => new(value);
    public static implicit operator PostgresValue(PhysicalAddress[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlInet[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlCidr[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlBox[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlLSeg[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlCircle[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlLine[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlPath[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlPolygon[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlTsVector[] value) => new(value);
    public static implicit operator PostgresValue(NpgsqlTsQuery[] value) => new(value);
    public static implicit operator PostgresValue(BigInteger[] value) => new(value);
    public static implicit operator PostgresValue(char[] value) => new(value);
}
