using System.Data;
using Xunit;

namespace StringThing.SqlClient.Tests;

public class SqlServerValueTests
{
    public static TheoryData<SqlServerValue, object?, SqlDbType> ScalarRoundTripData => new()
    {
        { true, true, SqlDbType.Bit },
        { false, false, SqlDbType.Bit },
        { (byte)42, (byte)42, SqlDbType.TinyInt },
        { (short)42, (short)42, SqlDbType.SmallInt },
        { 42, 42, SqlDbType.Int },
        { 42L, 42L, SqlDbType.BigInt },
        { 3.14f, 3.14f, SqlDbType.Real },
        { 2.71828, 2.71828, SqlDbType.Float },
        { 99.99m, 99.99m, SqlDbType.Decimal },
        { 'A', 'A', SqlDbType.NChar },
        { Guid.Parse("12345678-1234-1234-1234-123456789012"), Guid.Parse("12345678-1234-1234-1234-123456789012"), SqlDbType.UniqueIdentifier },
        { new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc), SqlDbType.DateTime2 },
        { new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.FromHours(2)), new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.FromHours(2)), SqlDbType.DateTimeOffset },
        { new DateOnly(2026, 4, 7), new DateOnly(2026, 4, 7), SqlDbType.Date },
        { new TimeOnly(12, 30, 45), new TimeOnly(12, 30, 45), SqlDbType.Time },
        { TimeSpan.FromHours(2), TimeSpan.FromHours(2), SqlDbType.Time },
        { "hello", "hello", SqlDbType.NVarChar },
    };

    [Theory]
    [MemberData(nameof(ScalarRoundTripData))]
    public void ToSqlParameter_RoundTripsValueAndType(SqlServerValue serverValue, object? expectedValue, SqlDbType expectedType)
    {
        // Act
        var parameter = serverValue.ToSqlParameter();

        // Assert
        Assert.Equal(expectedValue, parameter.Value);
        Assert.Equal(expectedType, parameter.SqlDbType);
    }

    [Fact]
    public void ToSqlParameter_NullString_ProducesDbNull()
    {
        // Arrange
        string? value = null;
        SqlServerValue serverValue = value;

        // Act
        var parameter = serverValue.ToSqlParameter();

        // Assert
        Assert.Equal(DBNull.Value, parameter.Value);
        Assert.Equal(SqlDbType.NVarChar, parameter.SqlDbType);
    }

    [Fact]
    public void ToSqlParameter_NullByteArray_ProducesDbNull()
    {
        // Arrange
        byte[]? value = null;
        SqlServerValue serverValue = value;

        // Act
        var parameter = serverValue.ToSqlParameter();

        // Assert
        Assert.Equal(DBNull.Value, parameter.Value);
        Assert.Equal(SqlDbType.VarBinary, parameter.SqlDbType);
    }

    [Fact]
    public void ToSqlParameter_ByteArray_PreservesValue()
    {
        // Arrange
        byte[] value = [0x01, 0x02, 0x03];
        SqlServerValue serverValue = value;

        // Act
        var parameter = serverValue.ToSqlParameter();

        // Assert
        Assert.Same(value, parameter.Value);
        Assert.Equal(SqlDbType.VarBinary, parameter.SqlDbType);
    }
}
