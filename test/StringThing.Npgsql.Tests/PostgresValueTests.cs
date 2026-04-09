using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using NpgsqlTypes;
using Xunit;

namespace StringThing.Npgsql.Tests;

public class PostgresValueTests
{
    public static TheoryData<PostgresValue, object?> ScalarRoundTripData => new()
    {
        { true, true },
        { false, false },
        { (short)42, (short)42 },
        { 42, 42 },
        { 42L, 42L },
        { 3.14f, 3.14f },
        { 2.71828, 2.71828 },
        { 99.99m, 99.99m },
        { 'A', 'A' },
        { Guid.Parse("12345678-1234-1234-1234-123456789012"), Guid.Parse("12345678-1234-1234-1234-123456789012") },
        { new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc) },
        { new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.FromHours(2)), new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.FromHours(2)) },
        { new DateOnly(2026, 4, 7), new DateOnly(2026, 4, 7) },
        { new TimeOnly(12, 30, 45), new TimeOnly(12, 30, 45) },
        { TimeSpan.FromHours(36), TimeSpan.FromHours(36) },
        { new NpgsqlPoint(1.5, 2.5), new NpgsqlPoint(1.5, 2.5) },
        { "hello", "hello" },
        { BigInteger.Parse("99999999999999999999"), BigInteger.Parse("99999999999999999999") },
    };

    [Theory]
    [MemberData(nameof(ScalarRoundTripData))]
    public void ToNpgsqlParameter_RoundTripsScalarValue(PostgresValue postgresValue, object? expectedValue)
    {
        // Act
        var parameter = postgresValue.ToNpgsqlParameter();

        // Assert
        Assert.Equal(expectedValue, parameter.Value);
    }

    public static TheoryData<PostgresValue, object?> ReferenceTypeRoundTripData
    {
        get
        {
            var data = new TheoryData<PostgresValue, object?>();

            var bytes = new byte[] { 0x01, 0x02, 0x03 };
            data.Add(bytes, bytes);

            var bits = new BitArray([true, false, true]);
            data.Add(bits, bits);

            var ip = IPAddress.Parse("192.168.1.1");
            data.Add(ip, ip);

            var mac = PhysicalAddress.Parse("00-11-22-33-44-55");
            data.Add(mac, mac);

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ReferenceTypeRoundTripData))]
    public void ToNpgsqlParameter_RoundTripsReferenceTypeValue(PostgresValue postgresValue, object? expectedValue)
    {
        // Act
        var parameter = postgresValue.ToNpgsqlParameter();

        // Assert
        Assert.Same(expectedValue, parameter.Value);
    }

    public static TheoryData<PostgresValue, object?, Type> BoxedValueTypeRoundTripData
    {
        get
        {
            var data = new TheoryData<PostgresValue, object?, Type>();

            data.Add(new NpgsqlInet(IPAddress.Loopback, 32), new NpgsqlInet(IPAddress.Loopback, 32), typeof(NpgsqlInet));
            data.Add(new NpgsqlBox(1, 2, 0, 0), new NpgsqlBox(1, 2, 0, 0), typeof(NpgsqlBox));
            data.Add(new NpgsqlLSeg(0, 0, 1, 1), new NpgsqlLSeg(0, 0, 1, 1), typeof(NpgsqlLSeg));
            data.Add(new NpgsqlCircle(1, 2, 3), new NpgsqlCircle(1, 2, 3), typeof(NpgsqlCircle));
            data.Add(new NpgsqlLine(1, 2, 3), new NpgsqlLine(1, 2, 3), typeof(NpgsqlLine));
            data.Add(new NpgsqlRange<int>(1, 10), new NpgsqlRange<int>(1, 10), typeof(NpgsqlRange<int>));
            data.Add(new NpgsqlRange<DateOnly>(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                     new NpgsqlRange<DateOnly>(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                     typeof(NpgsqlRange<DateOnly>));

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(BoxedValueTypeRoundTripData))]
    public void ToNpgsqlParameter_RoundTripsBoxedValueType(PostgresValue postgresValue, object? expectedValue, Type expectedType)
    {
        // Act
        var parameter = postgresValue.ToNpgsqlParameter();

        // Assert
        Assert.Equal(expectedValue, parameter.Value);
        Assert.IsType(expectedType, parameter.Value);
    }

    public static TheoryData<PostgresValue, object?, Type> ArrayRoundTripData
    {
        get
        {
            var data = new TheoryData<PostgresValue, object?, Type>();

            data.Add(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, typeof(int[]));
            data.Add(new[] { "a", "b" }, new[] { "a", "b" }, typeof(string[]));
            data.Add(new[] { 1L, 2L }, new[] { 1L, 2L }, typeof(long[]));
            data.Add(new[] { true, false }, new[] { true, false }, typeof(bool[]));
            data.Add(new[] { 1.5, 2.5 }, new[] { 1.5, 2.5 }, typeof(double[]));
            data.Add(new[] { IPAddress.Loopback }, new[] { IPAddress.Loopback }, typeof(IPAddress[]));
            data.Add(new[] { 'A', 'B' }, new[] { 'A', 'B' }, typeof(char[]));
            data.Add(new[] { (short)1, (short)2 }, new[] { (short)1, (short)2 }, typeof(short[]));
            data.Add(new[] { 1.5f, 2.5f }, new[] { 1.5f, 2.5f }, typeof(float[]));
            data.Add(new[] { 99.99m }, new[] { 99.99m }, typeof(decimal[]));
            data.Add(new[] { Guid.Empty }, new[] { Guid.Empty }, typeof(Guid[]));
            data.Add(new[] { new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, new[] { new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, typeof(DateTime[]));
            data.Add(new[] { new DateOnly(2026, 1, 1) }, new[] { new DateOnly(2026, 1, 1) }, typeof(DateOnly[]));
            data.Add(new[] { new TimeOnly(12, 0) }, new[] { new TimeOnly(12, 0) }, typeof(TimeOnly[]));
            data.Add(new[] { TimeSpan.FromHours(1) }, new[] { TimeSpan.FromHours(1) }, typeof(TimeSpan[]));
            data.Add(new[] { new NpgsqlPoint(1, 2) }, new[] { new NpgsqlPoint(1, 2) }, typeof(NpgsqlPoint[]));
            data.Add(new[] { new NpgsqlBox(1, 2, 0, 0) }, new[] { new NpgsqlBox(1, 2, 0, 0) }, typeof(NpgsqlBox[]));
            data.Add(new[] { new NpgsqlCircle(1, 2, 3) }, new[] { new NpgsqlCircle(1, 2, 3) }, typeof(NpgsqlCircle[]));
            data.Add(new[] { new NpgsqlLine(1, 2, 3) }, new[] { new NpgsqlLine(1, 2, 3) }, typeof(NpgsqlLine[]));
            data.Add(new[] { new NpgsqlLSeg(0, 0, 1, 1) }, new[] { new NpgsqlLSeg(0, 0, 1, 1) }, typeof(NpgsqlLSeg[]));
            data.Add(new[] { new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)) }, new[] { new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)) }, typeof(DateTimeOffset[]));
            data.Add(new[] { new NpgsqlInterval(1, 2, 3) }, new[] { new NpgsqlInterval(1, 2, 3) }, typeof(NpgsqlInterval[]));
            data.Add(new[] { new NpgsqlInet(IPAddress.Loopback, 32) }, new[] { new NpgsqlInet(IPAddress.Loopback, 32) }, typeof(NpgsqlInet[]));
            data.Add(new[] { new NpgsqlCidr(IPAddress.Loopback, 24) }, new[] { new NpgsqlCidr(IPAddress.Loopback, 24) }, typeof(NpgsqlCidr[]));
            data.Add(new[] { new NpgsqlPath(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1)) }, new[] { new NpgsqlPath(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1)) }, typeof(NpgsqlPath[]));
            data.Add(new[] { new NpgsqlPolygon(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 0), new NpgsqlPoint(0, 1)) }, new[] { new NpgsqlPolygon(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 0), new NpgsqlPoint(0, 1)) }, typeof(NpgsqlPolygon[]));
            data.Add(new[] { PhysicalAddress.Parse("00-11-22-33-44-55") }, new[] { PhysicalAddress.Parse("00-11-22-33-44-55") }, typeof(PhysicalAddress[]));
            data.Add(new[] { BigInteger.Parse("999999999") }, new[] { BigInteger.Parse("999999999") }, typeof(BigInteger[]));

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ArrayRoundTripData))]
    public void ToNpgsqlParameter_RoundTripsArrayValue(PostgresValue postgresValue, object? expectedValue, Type expectedType)
    {
        // Act
        var parameter = postgresValue.ToNpgsqlParameter();

        // Assert
        Assert.Equal(expectedValue, parameter.Value);
        Assert.IsType(expectedType, parameter.Value);
    }

    [Fact]
    public void ToNpgsqlParameter_DbNull_ProducesDbNullValue()
    {
        // Act
        var parameter = PostgresValue.Null.ToNpgsqlParameter();

        // Assert
        Assert.Equal(DBNull.Value, parameter.Value);
    }

    public static TheoryData<PostgresValue> NullReferenceTypeData => new()
    {
        (string?)null,
        (byte[]?)null,
        (BitArray?)null,
        (IPAddress?)null,
        (PhysicalAddress?)null,
        (NpgsqlTsVector?)null,
        (NpgsqlTsQuery?)null,
    };

    [Theory]
    [MemberData(nameof(NullReferenceTypeData))]
    public void ToNpgsqlParameter_NullReferenceType_ProducesDbNull(PostgresValue postgresValue)
    {
        // Act
        var parameter = postgresValue.ToNpgsqlParameter();

        // Assert
        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void ToNpgsqlParameter_DateTimePreservesKind()
    {
        // Arrange
        var utc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var local = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        // Act
        var utcParam = ((PostgresValue)utc).ToNpgsqlParameter();
        var localParam = ((PostgresValue)local).ToNpgsqlParameter();
        var unspecifiedParam = ((PostgresValue)unspecified).ToNpgsqlParameter();

        // Assert
        Assert.Equal(DateTimeKind.Utc, ((DateTime)utcParam.Value!).Kind);
        Assert.Equal(DateTimeKind.Local, ((DateTime)localParam.Value!).Kind);
        Assert.Equal(DateTimeKind.Unspecified, ((DateTime)unspecifiedParam.Value!).Kind);
    }

    private record TestJsonType(string Name, int Age) : IPostgresJson
    {
        public string ToJson() => $$"""
            {"name":"{{Name}}","age":{{Age}}}
            """.Trim();
    }

    [Fact]
    public void ToNpgsqlParameter_IPostgresJson_ProducesJsonbParameter()
    {
        // Arrange
        var value = new TestJsonType("alice", 30);

        // Act
        var postgresValue = new PostgresValue(value);
        var parameter = postgresValue.ToNpgsqlParameter();

        // Assert
        Assert.Equal("""{"name":"alice","age":30}""", parameter.Value);
        Assert.Equal(NpgsqlDbType.Jsonb, parameter.NpgsqlDbType);
    }

    [Fact]
    public void ToNpgsqlParameter_IPostgresJsonArray_ProducesJsonbArrayParameter()
    {
        // Arrange
        TestJsonType[] values = [new("alice", 30), new("bob", 25)];

        // Act
        var postgresValue = new PostgresValue(values);
        var parameter = postgresValue.ToNpgsqlParameter();

        // Assert
        var strings = (string[])parameter.Value!;
        Assert.Equal("""{"name":"alice","age":30}""", strings[0]);
        Assert.Equal("""{"name":"bob","age":25}""", strings[1]);
    }

    [Fact]
    public void WhenInterpolatingIPostgresJson_CapturesAsJsonbParameter()
    {
        // Arrange
        var user = new TestJsonType("alice", 30);

        // Act
        PostgresSql stmt = $"INSERT INTO t (data) VALUES ({user})";

        // Assert
        Assert.Equal("INSERT INTO t (data) VALUES ($1)", stmt.Sql);
        Assert.Equal("""{"name":"alice","age":30}""", stmt.Parameters[0].Value);
    }
}
