using Microsoft.Data.Sqlite;

namespace StringThing.Sqlite;

public readonly record struct SqliteValue
{
    public object? Value { get; }

    internal SqliteValue(object? value)
    {
        Value = value;
    }

    public SqliteParameter ToSqliteParameter()
    {
        return new SqliteParameter { Value = Value ?? DBNull.Value };
    }

    // --- Numeric ---

    public static implicit operator SqliteValue(bool value) => new(value);
    public static implicit operator SqliteValue(byte value) => new(value);
    public static implicit operator SqliteValue(short value) => new(value);
    public static implicit operator SqliteValue(int value) => new(value);
    public static implicit operator SqliteValue(long value) => new(value);
    public static implicit operator SqliteValue(float value) => new(value);
    public static implicit operator SqliteValue(double value) => new(value);
    public static implicit operator SqliteValue(decimal value) => new(value);

    // --- Text ---

    public static implicit operator SqliteValue(char value) => new(value);
    public static implicit operator SqliteValue(string? value) => value is not null
        ? new(value)
        : new(DBNull.Value);

    // --- UUID ---

    public static implicit operator SqliteValue(Guid value) => new(value);

    // --- Date / Time ---

    public static implicit operator SqliteValue(DateTime value) => new(value);
    public static implicit operator SqliteValue(DateTimeOffset value) => new(value);
    public static implicit operator SqliteValue(DateOnly value) => new(value);
    public static implicit operator SqliteValue(TimeOnly value) => new(value);
    public static implicit operator SqliteValue(TimeSpan value) => new(value);

    // --- Binary ---

    public static implicit operator SqliteValue(byte[]? value) => value is not null
        ? new(value)
        : new(DBNull.Value);
}
