using Microsoft.Data.Sqlite;
using StringThing.UnsafeSql;
using Xunit;

namespace StringThing.Sqlite.IntegrationTests;

public class SqliteIntegrationTests
{
    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var createTable = connection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT,
                active INTEGER NOT NULL DEFAULT 1
            )
            """;
        createTable.ExecuteNonQuery();

        using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO users (id, name, email, active) VALUES
                (1, 'alice', 'alice@example.com', 1),
                (2, 'bob', NULL, 1),
                (3, 'carol', 'carol@example.com', 0)
            """;
        insert.ExecuteNonQuery();

        return connection;
    }

    [Fact]
    public void WhenQueryingWithScalarParameter_ReturnsMatchingRow()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;
        Sqlite stmt = $"SELECT name FROM users WHERE id = {userId}";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public void WhenQueryingWithNullParameter_MatchesNullColumn()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 2;
        Sqlite stmt = $"SELECT email FROM users WHERE id = {userId}";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public void WhenQueryingWithMultipleParameters_FiltersCorrectly()
    {
        // Arrange
        using var connection = CreateDatabase();
        var minId = 1;
        var active = true;
        Sqlite stmt = $"SELECT name FROM users WHERE id > {minId} AND active = {active} ORDER BY id";
        using var command = stmt.ToCommand(connection);

        // Act
        using var reader = command.ExecuteReader();

        // Assert
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        Assert.Equal(["bob"], names);
    }

    [Fact]
    public void WhenQueryingWithDeduplicatedParameter_ProducesCorrectResult()
    {
        // Arrange
        using var connection = CreateDatabase();
        var searchTerm = "alice";
        Sqlite stmt = $"SELECT name FROM users WHERE name = {searchTerm} OR email LIKE {searchTerm} || '%'";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public void WhenInsertingWithNullParameter_InsertsNull()
    {
        // Arrange
        using var connection = CreateDatabase();
        var id = 99;
        var name = "dave";
        string? email = null;
        Sqlite insertStmt = $"INSERT INTO users (id, name, email) VALUES ({id}, {name}, {email})";
        using var insertCmd = insertStmt.ToCommand(connection);
        insertCmd.ExecuteNonQuery();

        Sqlite selectStmt = $"SELECT email FROM users WHERE id = {id}";
        using var selectCmd = selectStmt.ToCommand(connection);

        // Act
        var result = selectCmd.ExecuteScalar();

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public void WhenUsingUnsafeSql_SplicesCorrectly()
    {
        // Arrange
        using var connection = CreateDatabase();
        var tableName = Sql.Unsafe("users");
        var userId = 3;
        Sqlite stmt = $"SELECT name FROM {tableName} WHERE id = {userId}";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal("carol", result);
    }

    [Fact]
    public void WhenUsingFragment_ComposesAndExecutesCorrectly()
    {
        // Arrange
        using var connection = CreateDatabase();
        var minId = 0;
        var active = true;
        SqliteFragment filter = $"id > {minId} AND active = {active}";
        Sqlite stmt = $"SELECT name FROM users WHERE {filter} ORDER BY id";
        using var command = stmt.ToCommand(connection);

        // Act
        using var reader = command.ExecuteReader();

        // Assert
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        Assert.Equal(["alice", "bob"], names);
    }

    // --- Multi-row insert ---

    private record InsertUser(int Id, string Name, string? Email) : ISqliteRow
    {
        public SqliteFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [Fact]
    public void WhenInsertingMultipleRowsWithISqliteRow_InsertsAllRows()
    {
        // Arrange
        using var connection = CreateDatabase();

        InsertUser[] users =
        [
            new(50, "eve", "eve@example.com"),
            new(51, "frank", null),
            new(52, "grace", "grace@example.com"),
        ];
        Sqlite stmt = $"INSERT INTO users (id, name, email) VALUES {Sqlite.InsertRows(users)}";
        using var command = stmt.ToCommand(connection);

        // Act
        command.ExecuteNonQuery();

        // Assert
        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT name FROM users WHERE id >= 50 AND id <= 52 ORDER BY id";
        using var reader = verify.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        Assert.Equal(["eve", "frank", "grace"], names);
    }
}
