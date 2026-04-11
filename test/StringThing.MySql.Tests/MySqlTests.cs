using MySqlConnector;
using StringThing.Core;
using StringThing.UnsafeSql;
using Xunit;

namespace StringThing.MySql.Tests;

public class MySqlTests
{
    private static object?[] Values(IReadOnlyList<MySqlParameter> parameters)
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
        MySql stmt = $"SELECT 1";

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
        MySql stmt = $"SELECT * FROM users WHERE id = {userId}";

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
        MySql stmt = $"SELECT * FROM users WHERE id = {user.Id}";

        // Assert
        Assert.Equal("SELECT * FROM users WHERE id = @user_Id", stmt.Sql);
    }

    [Fact]
    public void WhenInterpolatingSameVariableTwice_Deduplicates()
    {
        // Arrange
        var matchValue = 99;

        // Act
        MySql stmt = $"WHERE a = {matchValue} OR b = {matchValue}";

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
        MySql stmt = $"WHERE first = {firstName} AND last = {lastName}";

        // Assert
        Assert.Equal("WHERE first = @firstName AND last = @lastName", stmt.Sql);
        Assert.Equal(2, stmt.Parameters.Count);
    }

    [Fact]
    public void WhenInterpolatingNullString_ProducesDbNull()
    {
        // Arrange
        string? email = null;

        // Act
        MySql stmt = $"WHERE email = {email}";

        // Assert
        Assert.Equal("WHERE email = @email", stmt.Sql);
        Assert.Equal(DBNull.Value, stmt.Parameters[0].Value);
    }

    [Fact]
    public void WhenInterpolatingSqlUnsafe_SplicesRawText()
    {
        // Arrange
        var tableName = Sql.Unsafe("users");
        var userId = 1;

        // Act
        MySql stmt = $"SELECT * FROM {tableName} WHERE id = {userId}";

        // Assert
        Assert.Equal("SELECT * FROM users WHERE id = @userId", stmt.Sql);
        Assert.Single(stmt.Parameters);
    }

    [Fact]
    public void WhenSplicingFragment_ComposesCorrectly()
    {
        // Arrange
        var minAge = 18;
        var status = "active";
        MySqlFragment filter = $"age >= {minAge} AND status = {status}";

        // Act
        MySql stmt = $"SELECT * FROM users WHERE {filter}";

        // Assert
        Assert.Contains("@filter_minAge", stmt.Sql);
        Assert.Contains("@filter_status", stmt.Sql);
        Assert.Equal(2, stmt.Parameters.Count);
    }

    [Fact]
    public void WhenLiteralIsMissingWhitespace_ReproducesFaithfullyWithoutFixing()
    {
        // Arrange
        var value = 42;

        // Act
        MySql stmt = $"WHERE true OR{value}";

        // Assert
        Assert.Equal("WHERE true OR@value", stmt.Sql);
    }

    // --- InsertRows ---

    private record TestRow(int Id, string Name) : IMySqlRow
    {
        public MySqlFragment RowValues => $"({Id}, {Name})";
    }

    [Fact]
    public void InsertRows_WhenMultipleRows_ComposesCommaSeparatedValueFragments()
    {
        // Arrange
        TestRow[] rows = [new(1, "alice"), new(2, "bob"), new(3, "carol")];

        // Act
        MySql stmt = $"INSERT INTO t (id, name) VALUES {MySql.InsertRows(rows)}";

        // Assert
        Assert.Equal("INSERT INTO t (id, name) VALUES (@p0, @p1), (@p2, @p3), (@p4, @p5)", stmt.Sql);
        object[] expectedParameters = [1, "alice", 2, "bob", 3, "carol"];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InsertRows_WhenEmpty_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MySql.InsertRows(Array.Empty<TestRow>()));
    }

    // --- InList ---

    [Fact]
    public void InList_WhenCalledWithIntArray_ProducesParenthesizedList()
    {
        // Act
        MySql stmt = $"WHERE id IN {MySql.InList([1, 2, 3])}";

        // Assert
        Assert.Equal("WHERE id IN (@p0, @p1, @p2)", stmt.Sql);
        object[] expectedParameters = [1, 2, 3];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InList_WhenCalledWithStringArray_ProducesParenthesizedList()
    {
        // Act
        MySql stmt = $"WHERE name IN {MySql.InList(["alice", "bob"])}";

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
        MySql stmt = $"WHERE id IN {MySql.InList([.. ids])}";

        // Assert
        Assert.Equal("WHERE id IN (@p0, @p1, @p2)", stmt.Sql);
        object[] expectedParameters = [10, 20, 30];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InList_WhenEmpty_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MySql.InList([]));
    }
}
