using MySqlConnector;
using Xunit;

namespace StringThing.MySql.Tests;

public class MySqlValueTests
{
    public static TheoryData<MySqlValue, object?, MySqlDbType> ScalarRoundTripData => new()
    {
        { true, true, MySqlDbType.Bool },
        { false, false, MySqlDbType.Bool },
        { (byte)42, (byte)42, MySqlDbType.UByte },
        { (short)42, (short)42, MySqlDbType.Int16 },
        { 42, 42, MySqlDbType.Int32 },
        { 42L, 42L, MySqlDbType.Int64 },
        { 3.14f, 3.14f, MySqlDbType.Float },
        { 2.71828, 2.71828, MySqlDbType.Double },
        { 99.99m, 99.99m, MySqlDbType.Decimal },
        { 'A', 'A', MySqlDbType.VarChar },
        { Guid.Parse("12345678-1234-1234-1234-123456789012"), Guid.Parse("12345678-1234-1234-1234-123456789012"), MySqlDbType.Guid },
        { new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc), MySqlDbType.DateTime },
        { new DateOnly(2026, 4, 7), new DateOnly(2026, 4, 7), MySqlDbType.Date },
        { new TimeOnly(12, 30, 45), new TimeOnly(12, 30, 45), MySqlDbType.Time },
        { TimeSpan.FromHours(2), TimeSpan.FromHours(2), MySqlDbType.Time },
        { "hello", "hello", MySqlDbType.VarChar },
    };

    [Theory]
    [MemberData(nameof(ScalarRoundTripData))]
    public void ToMySqlParameter_RoundTripsValueAndType(MySqlValue mySqlValue, object? expectedValue, MySqlDbType expectedType)
    {
        // Act
        var parameter = mySqlValue.ToMySqlParameter();

        // Assert
        Assert.Equal(expectedValue, parameter.Value);
        Assert.Equal(expectedType, parameter.MySqlDbType);
    }

    [Fact]
    public void ToMySqlParameter_NullString_ProducesDbNull()
    {
        // Arrange
        string? value = null;
        MySqlValue mySqlValue = value;

        // Act
        var parameter = mySqlValue.ToMySqlParameter();

        // Assert
        Assert.Equal(DBNull.Value, parameter.Value);
        Assert.Equal(MySqlDbType.VarChar, parameter.MySqlDbType);
    }

    [Fact]
    public void ToMySqlParameter_NullByteArray_ProducesDbNull()
    {
        // Arrange
        byte[]? value = null;
        MySqlValue mySqlValue = value;

        // Act
        var parameter = mySqlValue.ToMySqlParameter();

        // Assert
        Assert.Equal(DBNull.Value, parameter.Value);
        Assert.Equal(MySqlDbType.Blob, parameter.MySqlDbType);
    }

    [Fact]
    public void ToMySqlParameter_ByteArray_PreservesValue()
    {
        // Arrange
        byte[] value = [0x01, 0x02, 0x03];
        MySqlValue mySqlValue = value;

        // Act
        var parameter = mySqlValue.ToMySqlParameter();

        // Assert
        Assert.Same(value, parameter.Value);
        Assert.Equal(MySqlDbType.Blob, parameter.MySqlDbType);
    }
}
