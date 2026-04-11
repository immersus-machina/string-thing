using Microsoft.Data.Sqlite;
using StringThing.Core;
using StringThing.UnsafeSql;
using Xunit;

namespace StringThing.Sqlite.Tests;

public class SqliteTests
{
    private static object?[] Values(IReadOnlyList<SqliteParameter> parameters)
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
        Sqlite stmt = $"SELECT 1";

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
        Sqlite stmt = $"SELECT * FROM users WHERE id = {userId}";

        // Assert
        Assert.Equal("SELECT * FROM users WHERE id = @userId", stmt.Sql);
        object[] expectedParameters = [42];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void WhenInterpolatingSameVariableTwice_Deduplicates()
    {
        // Arrange
        var matchValue = 99;

        // Act
        Sqlite stmt = $"WHERE a = {matchValue} OR b = {matchValue}";

        // Assert
        Assert.Equal("WHERE a = @matchValue OR b = @matchValue", stmt.Sql);
        Assert.Single(stmt.Parameters);
    }

    [Fact]
    public void WhenInterpolatingNullString_ProducesDbNull()
    {
        // Arrange
        string? email = null;

        // Act
        Sqlite stmt = $"WHERE email = {email}";

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
        Sqlite stmt = $"SELECT * FROM {tableName} WHERE id = {userId}";

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
        SqliteFragment filter = $"age >= {minAge} AND status = {status}";

        // Act
        Sqlite stmt = $"SELECT * FROM users WHERE {filter}";

        // Assert
        Assert.Contains("@filter_minAge", stmt.Sql);
        Assert.Contains("@filter_status", stmt.Sql);
        Assert.Equal(2, stmt.Parameters.Count);
    }

    // --- InList ---

    [Fact]
    public void InList_WhenCalledWithIntArray_ProducesParenthesizedList()
    {
        // Act
        Sqlite stmt = $"WHERE id IN {Sqlite.InList([1, 2, 3])}";

        // Assert
        Assert.Equal("WHERE id IN (@p0, @p1, @p2)", stmt.Sql);
        object[] expectedParameters = [1, 2, 3];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

    [Fact]
    public void InList_WhenCalledWithSpreadList_Works()
    {
        // Arrange
        var ids = new List<int> { 10, 20, 30 };

        // Act
        Sqlite stmt = $"WHERE id IN {Sqlite.InList([.. ids])}";

        // Assert
        Assert.Equal("WHERE id IN (@p0, @p1, @p2)", stmt.Sql);
    }

    // --- InsertRows ---

    private record TestRow(int Id, string Name) : ISqliteRow
    {
        public SqliteFragment RowValues => $"({Id}, {Name})";
    }

    [Fact]
    public void InsertRows_WhenMultipleRows_ComposesCommaSeparatedValueFragments()
    {
        // Arrange
        TestRow[] rows = [new(1, "alice"), new(2, "bob"), new(3, "carol")];

        // Act
        Sqlite stmt = $"INSERT INTO t (id, name) VALUES {Sqlite.InsertRows(rows)}";

        // Assert
        Assert.Equal("INSERT INTO t (id, name) VALUES (@p0, @p1), (@p2, @p3), (@p4, @p5)", stmt.Sql);
        object[] expectedParameters = [1, "alice", 2, "bob", 3, "carol"];
        Assert.Equal(expectedParameters, Values(stmt.Parameters));
    }

}
