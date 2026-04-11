using MySqlConnector;

namespace StringThing.MySql;

public readonly record struct MySqlValue
{
    public object? Value { get; }
    public MySqlDbType Type { get; }

    internal MySqlValue(object? value, MySqlDbType type)
    {
        Value = value;
        Type = type;
    }

    public MySqlParameter ToMySqlParameter()
    {
        return new MySqlParameter { Value = Value ?? DBNull.Value, MySqlDbType = Type };
    }

    // --- Numeric ---

    public static implicit operator MySqlValue(bool value) => new(value, MySqlDbType.Bool);
    public static implicit operator MySqlValue(byte value) => new(value, MySqlDbType.UByte);
    public static implicit operator MySqlValue(short value) => new(value, MySqlDbType.Int16);
    public static implicit operator MySqlValue(int value) => new(value, MySqlDbType.Int32);
    public static implicit operator MySqlValue(long value) => new(value, MySqlDbType.Int64);
    public static implicit operator MySqlValue(float value) => new(value, MySqlDbType.Float);
    public static implicit operator MySqlValue(double value) => new(value, MySqlDbType.Double);
    public static implicit operator MySqlValue(decimal value) => new(value, MySqlDbType.Decimal);
    public static implicit operator MySqlValue(ushort value) => new(value, MySqlDbType.UInt16);
    public static implicit operator MySqlValue(uint value) => new(value, MySqlDbType.UInt32);
    public static implicit operator MySqlValue(ulong value) => new(value, MySqlDbType.UInt64);
    public static implicit operator MySqlValue(sbyte value) => new(value, MySqlDbType.Byte);

    // --- Text ---

    public static implicit operator MySqlValue(char value) => new(value, MySqlDbType.VarChar);
    public static implicit operator MySqlValue(string? value) => value is not null
        ? new(value, MySqlDbType.VarChar)
        : new(DBNull.Value, MySqlDbType.VarChar);

    // --- UUID ---

    public static implicit operator MySqlValue(Guid value) => new(value, MySqlDbType.Guid);

    // --- Date / Time ---

    public static implicit operator MySqlValue(DateTime value) => new(value, MySqlDbType.DateTime);
    public static implicit operator MySqlValue(DateTimeOffset value) => new(value, MySqlDbType.DateTime);
    public static implicit operator MySqlValue(DateOnly value) => new(value, MySqlDbType.Date);
    public static implicit operator MySqlValue(TimeOnly value) => new(value, MySqlDbType.Time);
    public static implicit operator MySqlValue(TimeSpan value) => new(value, MySqlDbType.Time);

    // --- Binary ---

    public static implicit operator MySqlValue(byte[]? value) => value is not null
        ? new(value, MySqlDbType.Blob)
        : new(DBNull.Value, MySqlDbType.Blob);
}
