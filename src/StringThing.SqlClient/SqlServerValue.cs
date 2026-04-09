using System.Data;
using Microsoft.Data.SqlClient;

namespace StringThing.SqlClient;

public readonly record struct SqlServerValue
{
    public object? Value { get; }
    public SqlDbType Type { get; }

    internal SqlServerValue(object? value, SqlDbType type)
    {
        Value = value;
        Type = type;
    }

    public SqlParameter ToSqlParameter()
    {
        return new SqlParameter { Value = Value ?? DBNull.Value, SqlDbType = Type };
    }

    public static SqlServerValue Null(SqlDbType type) => new(DBNull.Value, type);

    // --- Numeric ---

    public static implicit operator SqlServerValue(bool value) => new(value, SqlDbType.Bit);
    public static implicit operator SqlServerValue(byte value) => new(value, SqlDbType.TinyInt);
    public static implicit operator SqlServerValue(short value) => new(value, SqlDbType.SmallInt);
    public static implicit operator SqlServerValue(int value) => new(value, SqlDbType.Int);
    public static implicit operator SqlServerValue(long value) => new(value, SqlDbType.BigInt);
    public static implicit operator SqlServerValue(float value) => new(value, SqlDbType.Real);
    public static implicit operator SqlServerValue(double value) => new(value, SqlDbType.Float);
    public static implicit operator SqlServerValue(decimal value) => new(value, SqlDbType.Decimal);

    // --- Text ---

    public static implicit operator SqlServerValue(char value) => new(value, SqlDbType.NChar);
    public static implicit operator SqlServerValue(string? value) => value is not null
        ? new(value, SqlDbType.NVarChar)
        : new(DBNull.Value, SqlDbType.NVarChar);

    // --- UUID ---

    public static implicit operator SqlServerValue(Guid value) => new(value, SqlDbType.UniqueIdentifier);

    // --- Date / Time ---

    public static implicit operator SqlServerValue(DateTime value) => new(value, SqlDbType.DateTime2);
    public static implicit operator SqlServerValue(DateTimeOffset value) => new(value, SqlDbType.DateTimeOffset);
    public static implicit operator SqlServerValue(DateOnly value) => new(value, SqlDbType.Date);
    public static implicit operator SqlServerValue(TimeOnly value) => new(value, SqlDbType.Time);
    public static implicit operator SqlServerValue(TimeSpan value) => new(value, SqlDbType.Time);

    // --- Binary ---

    public static implicit operator SqlServerValue(byte[]? value) => value is not null
        ? new(value, SqlDbType.VarBinary)
        : new(DBNull.Value, SqlDbType.VarBinary);
}
