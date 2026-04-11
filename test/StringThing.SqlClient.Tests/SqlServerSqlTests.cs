using Microsoft.Data.SqlClient;
using Xunit;

namespace StringThing.SqlClient.Tests;

public class SqlServerSqlTests
{
    private static object?[] Values(IReadOnlyList<SqlParameter> parameters)
    {
        var result = new object?[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
            result[i] = parameters[i].Value;
        return result;
    }

    [Fact]
    public void WhenInterpolatingLiteralOnly_CapturesSqlAndProducesNoParameters()
    {
        // Act
        SqlServerSql stmt = $"SELECT 1";

        // Assert
        Assert.Equal("SELECT 1", stmt.Sql);
        Assert.Empty(stmt.Parameters);
    }

    [Fact]
    public void WhenInterpolatingSingleInteger_UsesNamedPlaceholder()
    {
        // Arrange
        var userId = 42;

        // Act
        SqlServerSql stmt = $"SELECT * FROM users WHERE id = {userId}";

        // Assert
        Assert.Equal("SELECT * FROM users WHERE id = @userId", stmt.Sql);
        object[] expectedParameters = [42];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingMemberAccess_ReplacesDotsWithUnderscores()
    {
        // Arrange
        var user = new { Id = 42 };

        // Act
        SqlServerSql stmt = $"SELECT * FROM users WHERE id = {user.Id}";

        // Assert
        Assert.Equal("SELECT * FROM users WHERE id = @user_Id", stmt.Sql);
    }

    [Fact]
    public void WhenInterpolatingSameVariableTwice_Deduplicates()
    {
        // Arrange
        var matchValue = 99;

        // Act
        SqlServerSql stmt = $"WHERE a = {matchValue} OR b = {matchValue}";

        // Assert
        Assert.Equal("WHERE a = @matchValue OR b = @matchValue", stmt.Sql);
        Assert.Single(stmt.Parameters);
    }

    [Fact]
    public void WhenInterpolatingDifferentVariables_ProducesDistinctParameters()
    {
        // Arrange
        var firstName = "alice";
        var lastName = "smith";

        // Act
        SqlServerSql stmt = $"WHERE first = {firstName} AND last = {lastName}";

        // Assert
        Assert.Equal("WHERE first = @firstName AND last = @lastName", stmt.Sql);
        Assert.Equal(2, stmt.Parameters.Count);
    }

    [Fact]
    public void WhenInterpolatingInlineLiteral_FallsBackToIndexedName()
    {
        // Act
        SqlServerSql stmt = $"SELECT * FROM users WHERE id = {42}";

        // Assert
        Assert.Equal("SELECT * FROM users WHERE id = @p0", stmt.Sql);
    }

    [Fact]
    public void WhenInterpolatingSqlUnsafe_SplicesRawText()
    {
        // Arrange
        var tableName = Sql.Unsafe("users");
        var userId = 1;

        // Act
        SqlServerSql stmt = $"SELECT * FROM {tableName} WHERE id = {userId}";

        // Assert
        Assert.Equal("SELECT * FROM users WHERE id = @userId", stmt.Sql);
        Assert.Single(stmt.Parameters);
    }

    [Fact]
    public void WhenInterpolatingMultipleTypes_CapturesAllCorrectly()
    {
        // Arrange
        var name = "alice";
        var age = 30;
        var active = true;

        // Act
        SqlServerSql stmt = $"WHERE name = {name} AND age = {age} AND active = {active}";

        // Assert
        Assert.Equal("WHERE name = @name AND age = @age AND active = @active", stmt.Sql);
        object[] expectedParameters = ["alice", 30, true];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingNullString_ProducesDbNull()
    {
        // Arrange
        string? email = null;

        // Act
        SqlServerSql stmt = $"WHERE email = {email}";

        // Assert
        Assert.Equal("WHERE email = @email", stmt.Sql);
        Assert.Equal(DBNull.Value, stmt.Parameters[0].Value);
    }

    [Fact]
    public void WhenUsingIndexedNamer_ProducesIndexedPlaceholders()
    {
        // Arrange
        var userId = 42;
        var name = "alice";

        // Act
        SqlServerStatement<IndexedParameterNamer> stmt = $"WHERE id = {userId} AND name = {name}";

        // Assert
        Assert.Equal("WHERE id = @p0 AND name = @p1", stmt.Sql);
    }

    [Fact]
    public void WhenSplicingFragment_ComposesCorrectly()
    {
        // Arrange
        var minAge = 18;
        var status = "active";
        SqlServerFragment filter = $"age >= {minAge} AND status = {status}";

        // Act
        SqlServerSql stmt = $"SELECT * FROM users WHERE {filter}";

        // Assert
        Assert.Contains("@filter_minAge", stmt.Sql);
        Assert.Contains("@filter_status", stmt.Sql);
        Assert.Equal(2, stmt.Parameters.Count);
    }

    [Fact]
    public void WhenVariableChangesAfterFragmentCreation_FragmentRetainsOriginalValue()
    {
        // Arrange
        var minAge = 18;
        SqlServerFragment filter = $"age >= {minAge}";
        minAge = 99;

        // Act
        SqlServerSql stmt = $"SELECT * FROM users WHERE {filter} OR age >= {minAge}";

        // Assert
        Assert.Equal(18, stmt.Parameters[0].Value);
        Assert.Equal(99, stmt.Parameters[1].Value);
    }

    [Fact]
    public void WhenLiteralIsMissingWhitespace_ReproducesFaithfullyWithoutFixing()
    {
        // Arrange
        var value = 42;

        // Act
        SqlServerSql stmt = $"WHERE true OR{value}";

        // Assert
        Assert.Equal("WHERE true OR@value", stmt.Sql);
    }

    [Fact]
    public void WhenUnderscoreInExpression_FallsBackToIndexed()
    {
        // Arrange
        var user_id = 42;

        // Act
        SqlServerSql stmt = $"WHERE id = {user_id}";

        // Assert
        Assert.Equal("WHERE id = @p0", stmt.Sql);
    }

    [Fact]
    public void WhenExpressionCollidesWithIndexedPlaceholder_FallsBackToIndexed()
    {
        // Arrange
        var p3 = 42;

        // Act
        SqlServerSql stmt = $"WHERE id = {p3}";

        // Assert
        Assert.Equal("WHERE id = @p0", stmt.Sql);
    }

    // --- InList ---

    [Fact]
    public void InList_WhenCalledWithIntArray_ProducesParenthesizedList()
    {
        // Act
        SqlServerSql stmt = $"WHERE id IN {SqlServerSql.InList([1, 2, 3])}";

        // Assert
        Assert.Equal("WHERE id IN (@p0, @p1, @p2)", stmt.Sql);
        object[] expectedParameters = [1, 2, 3];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InList_WhenCalledWithStringArray_ProducesParenthesizedList()
    {
        // Act
        SqlServerSql stmt = $"WHERE name IN {SqlServerSql.InList(["alice", "bob"])}";

        // Assert
        Assert.Equal("WHERE name IN (@p0, @p1)", stmt.Sql);
        object[] expectedParameters = ["alice", "bob"];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InList_WhenCalledWithSpreadList_Works()
    {
        // Arrange
        var ids = new List<int> { 10, 20, 30 };

        // Act
        SqlServerSql stmt = $"WHERE id IN {SqlServerSql.InList([.. ids])}";

        // Assert
        Assert.Equal("WHERE id IN (@p0, @p1, @p2)", stmt.Sql);
        object[] expectedParameters = [10, 20, 30];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InList_WhenEmpty_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SqlServerSql.InList([]));
    }
}
