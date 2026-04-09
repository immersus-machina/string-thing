using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace StringThing.Npgsql.Tests;

public class SqlStatementTests
{
    private static object?[] Values(IReadOnlyList<NpgsqlParameter> parameters)
    {
        var result = new object?[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
            result[i] = parameters[i].Value;
        return result;
    }

    private static T TypedValue<T>(NpgsqlParameter parameter) =>
        ((NpgsqlParameter<T>)parameter).TypedValue!;

    [Fact]
    public void WhenInterpolatingLiteralOnly_CapturesSqlAndProducesNoParameters()
    {
        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT 1";

        // Assert
        Assert.Equal("SELECT 1", stmt.Sql);
        Assert.Empty(stmt.Parameters);
    }

    [Fact]
    public void WhenInterpolatingSingleInteger_CapturesAsDollarOnePlaceholder()
    {
        // Arrange
        var userId = 42;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT * FROM users WHERE id = {userId}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM users WHERE id = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenLiteralIsMissingWhitespace_ReproducesFaithfullyWithoutFixing()
    {
        // Arrange
        var value = 42;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE true OR{value}";

        // Assert
        Assert.Equal("WHERE true OR$1", stmt.Sql);
    }

    [Fact]
    public void WhenInterpolatingSingleString_CapturesAsDollarOnePlaceholder()
    {
        // Arrange
        var name = "alice";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT * FROM users WHERE name = {name}";

        // Assert
        object[] expectedParameters = ["alice"];
        Assert.Equal("SELECT * FROM users WHERE name = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingMultipleIntegers_UsesIncrementingPositionalPlaceholders()
    {
        // Arrange
        var first = 1;
        var second = 2;
        var third = 3;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"a={first}, b={second}, c={third}";

        // Assert
        object[] expectedParameters = [1, 2, 3];
        Assert.Equal("a=$1, b=$2, c=$3", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingMixedTypes_CapturesInOrderWithCorrectTypes()
    {
        // Arrange
        var userId = 7;
        var name = "bob";
        var age = 30;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE id={userId} AND name={name} AND age={age}";

        // Assert
        object[] expectedParameters = [7, "bob", 30];
        Assert.Equal("WHERE id=$1 AND name=$2 AND age=$3", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenSameIntegerVariableInterpolatedTwice_DeduplicatesIntoSingleParameter()
    {
        // Arrange
        var matchValue = 99;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"a={matchValue} OR b={matchValue}";

        // Assert
        object[] expectedParameters = [99];
        Assert.Equal("a=$1 OR b=$1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenSameStringVariableInterpolatedTwice_DeduplicatesIntoSingleParameter()
    {
        // Arrange
        var searchTerm = "alice";

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"WHERE myOption.name = {searchTerm} OR yourOption.name = {searchTerm}";

        // Assert
        object[] expectedParameters = ["alice"];
        Assert.Equal("WHERE myOption.name = $1 OR yourOption.name = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenDifferentVariablesShareSameValue_DoesNotDeduplicate()
    {
        // Arrange
        var firstUserId = 99;
        var secondUserId = 99;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"a={firstUserId} OR b={secondUserId}";

        // Assert
        object[] expectedParameters = [99, 99];
        Assert.Equal("a=$1 OR b=$2", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingTwelveParameters_UsesTwoCharacterPlaceholdersForTenAndAbove()
    {
        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}";

        // Assert
        object[] expectedParameters = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        Assert.Equal("$1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingAllSupportedTypes_CapturesEachInOrderWithCorrectValues()
    {
        // Arrange
        bool boolValue = true;
        short shortValue = 100;
        int intValue = 42;
        long longValue = 9_000_000_000L;
        float floatValue = 3.14f;
        double doubleValue = 2.71828;
        decimal decimalValue = 99.99m;
        Guid guidValue = new("12345678-1234-1234-1234-123456789012");
        DateTime dateTimeValue = new(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc);
        DateTimeOffset dateTimeOffsetValue = new(2026, 4, 7, 12, 0, 0, TimeSpan.FromHours(2));
        DateOnly dateOnlyValue = new(2026, 4, 7);
        TimeOnly timeOnlyValue = new(12, 30, 45);
        string stringValue = "alice";
        byte[] byteArrayValue = [0x01, 0x02, 0x03];

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"{boolValue},{shortValue},{intValue},{longValue},{floatValue},{doubleValue},{decimalValue},{guidValue},{dateTimeValue},{dateTimeOffsetValue},{dateOnlyValue},{timeOnlyValue},{stringValue},{byteArrayValue}";

        // Assert
        Assert.Equal("$1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14", stmt.Sql);
        Assert.Equal(14, stmt.Parameters.Count);

        Assert.True(TypedValue<bool>(stmt.Parameters[0]));
        Assert.Equal((short)100, TypedValue<short>(stmt.Parameters[1]));
        Assert.Equal(42, TypedValue<int>(stmt.Parameters[2]));
        Assert.Equal(9_000_000_000L, TypedValue<long>(stmt.Parameters[3]));
        Assert.Equal(3.14f, TypedValue<float>(stmt.Parameters[4]));
        Assert.Equal(2.71828, TypedValue<double>(stmt.Parameters[5]));
        Assert.Equal(99.99m, TypedValue<decimal>(stmt.Parameters[6]));
        Assert.Equal(guidValue, TypedValue<Guid>(stmt.Parameters[7]));
        Assert.Equal(dateTimeValue, TypedValue<DateTime>(stmt.Parameters[8]));
        Assert.Equal(dateTimeOffsetValue, TypedValue<DateTimeOffset>(stmt.Parameters[9]));
        Assert.Equal(dateOnlyValue, TypedValue<DateOnly>(stmt.Parameters[10]));
        Assert.Equal(timeOnlyValue, TypedValue<TimeOnly>(stmt.Parameters[11]));
        Assert.Equal("alice", TypedValue<string>(stmt.Parameters[12]));
        Assert.Equal(byteArrayValue, TypedValue<byte[]>(stmt.Parameters[13]));
    }

    [Fact]
    public void WhenInterpolatingDateTimeWithDifferentKinds_PreservesEachKind()
    {
        // Arrange
        var utcDateTime = new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc);
        var unspecifiedDateTime = new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Unspecified);
        var localDateTime = new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Local);

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"{utcDateTime},{unspecifiedDateTime},{localDateTime}";

        // Assert
        Assert.Equal(DateTimeKind.Utc, TypedValue<DateTime>(stmt.Parameters[0]).Kind);
        Assert.Equal(DateTimeKind.Unspecified, TypedValue<DateTime>(stmt.Parameters[1]).Kind);
        Assert.Equal(DateTimeKind.Local, TypedValue<DateTime>(stmt.Parameters[2]).Kind);
    }

    [Fact]
    public void WhenInterpolatingByteArray_PreservesReferenceIdentity()
    {
        // Arrange
        byte[] payload = [0x01, 0x02, 0x03, 0x04];

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"{payload}";

        // Assert
        Assert.Same(payload, TypedValue<byte[]>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingSimpleVariable_CapturesExpressionTextAsParameterName()
    {
        // Arrange
        var userId = 42;

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM users WHERE id = {userId}";

        // Assert
        Assert.Single(stmt.ParameterNames);
        Assert.Equal("userId", stmt.ParameterNames[0]);
    }

    [Fact]
    public void WhenInterpolatingMultipleVariables_CapturesEachExpressionText()
    {
        // Arrange
        var customerId = 7;
        var minOrderAmount = 100m;
        var orderStatus = "shipped";

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"WHERE customer_id = {customerId} AND total >= {minOrderAmount} AND status = {orderStatus}";

        // Assert
        string?[] expectedNames = ["customerId", "minOrderAmount", "orderStatus"];
        Assert.Equal(expectedNames, stmt.ParameterNames);
    }

    [Fact]
    public void WhenInterpolatingSqlUnsafe_SplicesRawTextWithoutCreatingParameter()
    {
        // Arrange
        var orderByClause = Sql.Unsafe("ORDER BY created_at DESC");

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM users {orderByClause}";

        // Assert
        Assert.Equal("SELECT * FROM users ORDER BY created_at DESC", stmt.Sql);
        Assert.Empty(stmt.Parameters);
    }

    [Fact]
    public void WhenInterpolatingSqlUnsafeAlongsideParameter_NumbersOnlyTheParameter()
    {
        // Arrange
        var tableName = Sql.Unsafe("users");
        var userId = 42;

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM {tableName} WHERE id = {userId}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM users WHERE id = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingSqlUnsafeLongerThanInitialBuffer_GrowsWithoutOverflow()
    {
        // Arrange
        var longRawFragment = Sql.Unsafe(new string('x', 500));

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"{longRawFragment}";

        // Assert
        Assert.Equal(new string('x', 500), stmt.Sql);
        Assert.Empty(stmt.Parameters);
    }

    [Fact]
    public void WhenInterpolatingMultilineSqlUnsafeAroundParameters_SplicesBodyAndNumbersParameters()
    {
        // Arrange
        var usersTable = "users";
        var activeOnlyCte = Sql.Unsafe($"""
            WITH active_users AS (
                SELECT id, name, created_at
                FROM {usersTable}
                WHERE status = 'active'
                  AND deleted_at IS NULL
            )
            """);
        var minCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var namePrefix = "a";

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"{activeOnlyCte} SELECT * FROM active_users WHERE created_at >= {minCreatedAt} AND name LIKE {namePrefix}";

        // Assert
        const string expectedSql = """
            WITH active_users AS (
                SELECT id, name, created_at
                FROM users
                WHERE status = 'active'
                  AND deleted_at IS NULL
            ) SELECT * FROM active_users WHERE created_at >= $1 AND name LIKE $2
            """;
        Assert.Equal(expectedSql, stmt.Sql);
        Assert.Equal(2, stmt.Parameters.Count);
    }

    [Fact]
    public void WhenSameFunctionCallInterpolatedTwice_DoesNotDeduplicate()
    {
        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"a={GetValue()} OR b={GetValue()}";

        // Assert
        object[] expectedParameters = [1, 2];
        Assert.Equal("a=$1 OR b=$2", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    private int _callCounter;
    private int GetValue() => ++_callCounter;

    // --- Fragment splicing tests ---

    [Fact]
    public void WhenSplicingFragmentWithSingleParameter_PrefixesParameterNameWithFragmentVariable()
    {
        // Arrange
        var userId = 42;
        SqlFragment where = $"WHERE users.id = {userId}";

        // Act
        SqlStatement<NameCapturingParameterNamer> stmt = $"SELECT * FROM users {where}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM users WHERE users.id = @where_userId", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenSplicingFragmentUnderPostgresNamer_StillNumbersWithDollarPlaceholders()
    {
        // Arrange
        var userId = 42;
        SqlFragment where = $"WHERE users.id = {userId}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT * FROM users {where}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM users WHERE users.id = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenSameFragmentUsesSameVariableTwice_DeduplicatesViaPrefixedName()
    {
        // Arrange
        var userId = 42;
        SqlFragment filter =
            $"WHERE owner_id = {userId} OR creator_id = {userId}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT * FROM rows {filter}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM rows WHERE owner_id = $1 OR creator_id = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenTwoNamedFragmentsShareVariableName_DoesNotDeduplicateAcrossFragments()
    {
        // Arrange
        var userId = 42;
        var ownerId = 7;
        SqlFragment first = $"id = {userId}";
        SqlFragment second = $"id = {ownerId}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM rows WHERE {first} OR {second}";

        // Assert
        object[] expectedParameters = [42, 7];
        Assert.Equal("SELECT * FROM rows WHERE id = $1 OR id = $2", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenFragmentVariableUsedTwice_RecordedTwiceWithSamePrefixedName()
    {
        // Arrange
        var userId = 42;
        SqlFragment filter = $"id = {userId}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM rows WHERE {filter} OR exists ({filter})";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal(
            "SELECT * FROM rows WHERE id = $1 OR exists (id = $1)",
            stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenFragmentReturnedFromHelperMethod_DropsPrefixDueToFunctionCallExpression()
    {
        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM rows WHERE {WhereId(42)} AND {WhereId(99)}";

        // Assert
        object[] expectedParameters = [42, 99];
        Assert.Equal("SELECT * FROM rows WHERE id = $1 AND id = $2", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    private static SqlFragment WhereId(int id) => $"id = {id}";

    private static SqlFragment OwnerOrCreatorIs(int id) => $"owner = {id} OR creator = {id}";

    [Fact]
    public void WhenSplicingTwoInlineHelperCallsWithInternalDuplication_ProducesFourDistinctSlots()
    {
        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM rows WHERE {OwnerOrCreatorIs(1)} OR {OwnerOrCreatorIs(2)}";

        // Assert
        object[] expectedParameters = [1, 1, 2, 2];
        Assert.Equal(
            "SELECT * FROM rows WHERE owner = $1 OR creator = $2 OR owner = $3 OR creator = $4",
            stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenSplicingTwoAssignedHelperFragmentsWithInternalDuplication_DeduplicatesWithinEachButNotAcross()
    {
        // Arrange
        var firstFilter = OwnerOrCreatorIs(1);
        var secondFilter = OwnerOrCreatorIs(2);

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM rows WHERE {firstFilter} OR {secondFilter}";

        // Assert
        object[] expectedParameters = [1, 2];
        Assert.Equal(
            "SELECT * FROM rows WHERE owner = $1 OR creator = $1 OR owner = $2 OR creator = $2",
            stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenSplicingNestedFragments_ChainsPrefixesAcrossLevels()
    {
        // Arrange
        var userId = 42;
        SqlFragment inner = $"id = {userId}";
        SqlFragment outer = $"WHERE {inner}";

        // Act
        SqlStatement<NameCapturingParameterNamer> stmt = $"SELECT * FROM rows {outer}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM rows WHERE id = @outer_inner_userId", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenSplicingFragmentWithMultipleParameters_FlowsAllValuesAndLiteralsThroughInOrder()
    {
        // Arrange
        var minAge = 18;
        var status = "active";
        var minCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        SqlFragment filter = $"age >= {minAge} AND status = {status} AND created_at >= {minCreatedAt}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT * FROM users WHERE {filter}";

        // Assert
        Assert.Equal(
            "SELECT * FROM users WHERE age >= $1 AND status = $2 AND created_at >= $3",
            stmt.Sql);
        Assert.Equal(3, stmt.Parameters.Count);
    }

    // --- UnsafeSql inside fragments ---

    [Fact]
    public void WhenFragmentContainsUnsafeSql_SplicesRawTextIntoFragment()
    {
        // Arrange
        var tableName = Sql.Unsafe("users");
        var userId = 42;
        SqlFragment filter = $"SELECT * FROM {tableName} WHERE id = {userId}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"{filter}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM users WHERE id = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenFragmentContainsOnlyUnsafeSql_ProducesNoParameters()
    {
        // Arrange
        var orderBy = Sql.Unsafe("ORDER BY created_at DESC");
        SqlFragment fragment = $"{orderBy}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT * FROM users {fragment}";

        // Assert
        Assert.Equal("SELECT * FROM users ORDER BY created_at DESC", stmt.Sql);
        Assert.Empty(stmt.Parameters);
    }

    [Fact]
    public void WhenFragmentMixesUnsafeSqlAndNestedFragment_ComposesCorrectly()
    {
        // Arrange
        var userId = 42;
        var tableName = Sql.Unsafe("users");
        SqlFragment inner = $"id = {userId}";
        SqlFragment outer = $"SELECT * FROM {tableName} WHERE {inner}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"{outer}";

        // Assert
        object[] expectedParameters = [42];
        Assert.Equal("SELECT * FROM users WHERE id = $1", stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    // --- Empty and edge cases ---

    [Fact]
    public void WhenInterpolatingEmptyString_CapturesAsParameter()
    {
        // Arrange
        var empty = "";

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE name = {empty}";

        // Assert
        Assert.Equal("WHERE name = $1", stmt.Sql);
        Assert.Equal("", TypedValue<string>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingEmptyByteArray_CapturesAsParameter()
    {
        // Arrange
        byte[] empty = [];

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE data = {empty}";

        // Assert
        Assert.Equal("WHERE data = $1", stmt.Sql);
        Assert.Empty(TypedValue<byte[]>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingUnsafeSqlWithEmptyString_ProducesEmptySpan()
    {
        // Arrange
        var empty = Sql.Unsafe("");

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT 1{empty}";

        // Assert
        Assert.Equal("SELECT 1", stmt.Sql);
        Assert.Empty(stmt.Parameters);
    }

    [Fact]
    public void WhenMixingUnsafeSqlAndParametersAndFragments_ComposesCorrectly()
    {
        // Arrange
        var tableName = Sql.Unsafe("orders");
        var minAmount = 100m;
        SqlFragment statusFilter = $"status = {"shipped"}";

        // Act
        SqlStatement<PostgresParameterNamer> stmt =
            $"SELECT * FROM {tableName} WHERE amount > {minAmount} AND {statusFilter}";

        // Assert
        object[] expectedParameters = [100m, "shipped"];
        Assert.Equal(
            "SELECT * FROM orders WHERE amount > $1 AND status = $2",
            stmt.Sql);
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    // --- Tier 2: additional standard .NET types ---

    [Fact]
    public void WhenInterpolatingTimeSpan_CapturesAsParameter()
    {
        // Arrange
        var duration = TimeSpan.FromHours(36);

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE elapsed > {duration}";

        // Assert
        Assert.Equal("WHERE elapsed > $1", stmt.Sql);
        Assert.Equal(TimeSpan.FromHours(36), TypedValue<TimeSpan>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingIPAddress_CapturesAsParameter()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE client_ip = {ip}";

        // Assert
        Assert.Equal("WHERE client_ip = $1", stmt.Sql);
        Assert.Equal(ip, TypedValue<IPAddress>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingPhysicalAddress_CapturesAsParameter()
    {
        // Arrange
        var mac = PhysicalAddress.Parse("00-11-22-33-44-55");

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE mac_addr = {mac}";

        // Assert
        Assert.Equal("WHERE mac_addr = $1", stmt.Sql);
        Assert.Equal(mac, TypedValue<PhysicalAddress>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingBigInteger_CapturesAsParameter()
    {
        // Arrange
        var big = BigInteger.Parse("99999999999999999999");

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE amount = {big}";

        // Assert
        Assert.Equal("WHERE amount = $1", stmt.Sql);
        Assert.Equal(big, TypedValue<BigInteger>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingChar_CapturesAsParameter()
    {
        // Arrange
        var letter = 'A';

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE grade = {letter}";

        // Assert
        Assert.Equal("WHERE grade = $1", stmt.Sql);
        Assert.Equal('A', TypedValue<char>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingBitArray_CapturesAsParameter()
    {
        // Arrange
        var bits = new BitArray([true, false, true]);

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE flags = {bits}";

        // Assert
        Assert.Equal("WHERE flags = $1", stmt.Sql);
        Assert.Same(bits, TypedValue<BitArray>(stmt.Parameters[0]));
    }

    // --- Nullable parameters ---

    [Fact]
    public void WhenInterpolatingNullableIntWithValue_CapturesValue()
    {
        // Arrange
        int? userId = 42;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE id = {userId}";

        // Assert
        Assert.Equal("WHERE id = $1", stmt.Sql);
        Assert.Equal(42, stmt.Parameters[0].Value);
    }

    [Fact]
    public void WhenInterpolatingNullableIntWithNull_CapturesDbNull()
    {
        // Arrange
        int? userId = null;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE id = {userId}";

        // Assert
        Assert.Equal("WHERE id = $1", stmt.Sql);
        Assert.Equal(DBNull.Value, stmt.Parameters[0].Value);
    }

    [Fact]
    public void WhenInterpolatingNullableTimeSpanWithNull_CapturesDbNull()
    {
        // Arrange
        TimeSpan? duration = null;

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE elapsed > {duration}";

        // Assert
        Assert.Equal("WHERE elapsed > $1", stmt.Sql);
        Assert.Equal(DBNull.Value, stmt.Parameters[0].Value);
    }

    // --- Array parameters ---

    [Fact]
    public void WhenInterpolatingIntArray_CapturesAsArrayParameter()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE id = ANY({ids})";

        // Assert
        int[] expectedParameters = [1, 2, 3];
        Assert.Equal("WHERE id = ANY($1)", stmt.Sql);
        Assert.Equal(expectedParameters, TypedValue<int[]>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingStringList_CapturesAsArrayParameter()
    {
        // Arrange
        string[] tags = ["a", "b", "c"];

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE tags @> {tags}";

        // Assert
        string[] expectedParameters = ["a", "b", "c"];
        Assert.Equal("WHERE tags @> $1", stmt.Sql);
        Assert.Equal(expectedParameters, TypedValue<string[]>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingIPAddressArray_CapturesAsArrayParameter()
    {
        // Arrange
        IPAddress[] ips = [IPAddress.Loopback, IPAddress.IPv6Loopback];

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE ip = ANY({ips})";

        // Assert
        Assert.Equal("WHERE ip = ANY($1)", stmt.Sql);
        var captured = TypedValue<IPAddress[]>(stmt.Parameters[0]);
        Assert.Equal(2, captured.Length);
        Assert.Equal(IPAddress.Loopback, captured[0]);
    }

    // --- Npgsql-specific types ---

    [Fact]
    public void WhenInterpolatingNpgsqlPoint_CapturesAsParameter()
    {
        // Arrange
        var point = new NpgsqlPoint(1.5, 2.5);

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE location = {point}";

        // Assert
        Assert.Equal("WHERE location = $1", stmt.Sql);
        Assert.Equal(point, TypedValue<NpgsqlPoint>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingNpgsqlRangeInt_CapturesAsParameter()
    {
        // Arrange
        var range = new NpgsqlRange<int>(1, 10);

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE val <@ {range}";

        // Assert
        Assert.Equal("WHERE val <@ $1", stmt.Sql);
        Assert.Equal(range, TypedValue<NpgsqlRange<int>>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingNpgsqlRangeDateOnly_CapturesAsParameter()
    {
        // Arrange
        var checkIn = new DateOnly(2026, 6, 1);
        var checkOut = new DateOnly(2026, 6, 7);
        var range = new NpgsqlRange<DateOnly>(checkIn, checkOut);

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE stay && {range}";

        // Assert
        Assert.Equal("WHERE stay && $1", stmt.Sql);
        Assert.Equal(range, TypedValue<NpgsqlRange<DateOnly>>(stmt.Parameters[0]));
    }

    [Fact]
    public void WhenInterpolatingNpgsqlPointTwice_DeduplicatesByVariable()
    {
        // Arrange
        var point = new NpgsqlPoint(1, 2);

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE a = {point} OR b = {point}";

        // Assert
        Assert.Equal("WHERE a = $1 OR b = $1", stmt.Sql);
        Assert.Single(stmt.Parameters);
    }

    // --- Large-scale composition stress tests ---

    [Fact]
    public void WhenNestingFragmentsDeeplyWithAccumulatingParameters_HandlesHundredsOfParameters()
    {
        // Arrange
        // Level 0: "p0 = $1"
        // Level 1: "p0 = $1 AND p1 = $2"
        // ...
        // Level 199: "p0 = $1 AND p1 = $2 AND ... AND p199 = $200"
        const int depth = 200;
        SqlFragment current = $"{Sql.Unsafe("p0")} = {0}";
        for (var i = 1; i < depth; i++)
        {
            var columnName = Sql.Unsafe($"p{i}");
            var next = i;
            var previous = current;
            current = $"{previous} AND {columnName} = {next}";
        }

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"WHERE {current}";

        // Assert
        Assert.Equal(depth, stmt.Parameters.Count);
        for (var i = 0; i < depth; i++)
        {
            Assert.Equal(i, stmt.Parameters[i].Value);
        }
        Assert.Contains("$1", stmt.Sql);
        Assert.Contains("$200", stmt.Sql);
    }

    [Fact]
    public void WhenNestingFragmentsDeeply_SqlLiteralTextIsPreservedAcrossAllLevels()
    {
        // Arrange
        const int depth = 100;
        SqlFragment current = $"{Sql.Unsafe("col0")} = {0}";
        for (var i = 1; i < depth; i++)
        {
            var columnName = Sql.Unsafe($"col{i}");
            var next = i;
            var previous = current;
            current = $"{previous} OR {columnName} = {next}";
        }

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT * FROM t WHERE {current}";

        // Assert
        var sql = stmt.Sql;
        Assert.StartsWith("SELECT * FROM t WHERE col0 = $1", sql);
        Assert.Contains($"col99 = ${depth}", sql);
        Assert.Equal(depth, stmt.Parameters.Count);
    }

    [Fact]
    public void WhenNestingFragmentsWithMultipleParametersPerLevel_GrowsBuffersCorrectly()
    {
        // Arrange
        const int depth = 50;
        var a = 0;
        var b = 1;
        var c = 2;
        var d = 3;
        SqlFragment current = $"({a},{b},{c},{d})";
        for (var i = 1; i < depth; i++)
        {
            a = i * 4;
            b = i * 4 + 1;
            c = i * 4 + 2;
            d = i * 4 + 3;
            var previous = current;
            current = $"{previous},({a},{b},{c},{d})";
        }

        // Act
        SqlStatement<PostgresParameterNamer> stmt = $"SELECT {current}";

        // Assert
        Assert.Equal(depth * 4, stmt.Parameters.Count);
        Assert.Equal(0, stmt.Parameters[0].Value);
        Assert.Equal(199, stmt.Parameters[^1].Value);
    }

    // --- InsertRows ---

    private record TestRow(int Id, string Name) : IPostgresRow
    {
        public SqlFragment RowValues => $"({Id}, {Name})";
    }

    [Fact]
    public void InsertRows_WhenMultipleRows_ComposesCommaSeparatedValueFragments()
    {
        // Arrange
        TestRow[] rows = [new(1, "alice"), new(2, "bob"), new(3, "carol")];

        // Act
        PostgresSql stmt = $"INSERT INTO t (id, name) VALUES {PostgresSql.InsertRows(rows)}";

        // Assert
        Assert.Equal("INSERT INTO t (id, name) VALUES ($1, $2), ($3, $4), ($5, $6)", stmt.Sql);
        object[] expectedParameters = [1, "alice", 2, "bob", 3, "carol"];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InsertRows_WhenSingleRow_ProducesSingleValueGroup()
    {
        // Arrange
        TestRow[] rows = [new(1, "alice")];

        // Act
        PostgresSql stmt = $"INSERT INTO t (id, name) VALUES {PostgresSql.InsertRows(rows)}";

        // Assert
        Assert.Equal("INSERT INTO t (id, name) VALUES ($1, $2)", stmt.Sql);
        object[] expectedParameters = [1, "alice"];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InsertRows_WhenEmptyList_Throws()
    {
        // Arrange
        TestRow[] rows = [];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PostgresSql.InsertRows(rows));
    }
}
