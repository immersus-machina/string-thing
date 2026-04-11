using Microsoft.Data.Sqlite;
using StringThing.Sqlite.Dapper;
using Xunit;

namespace StringThing.Sqlite.Dapper.IntegrationTests;

public class SqliteDapperTests
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
                email TEXT
            )
            """;
        createTable.ExecuteNonQuery();

        using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO users (id, name, email) VALUES
                (1, 'alice', 'alice@example.com'),
                (2, 'bob', NULL),
                (3, 'carol', 'carol@example.com')
            """;
        insert.ExecuteNonQuery();

        return connection;
    }

    private record User(long Id, string Name, string? Email);

    [Fact]
    public void QueryStringSingle_ReturnsMappedObject()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<User>(
            $"SELECT id, name, email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal("alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public void QueryString_ReturnsMultipleMappedObjects()
    {
        // Arrange
        using var connection = CreateDatabase();
        var maxId = 3;

        // Act
        var users = connection.QueryString<User>(
            $"SELECT id, name, email FROM users WHERE id <= {maxId} ORDER BY id").ToList();

        // Assert
        Assert.Equal(3, users.Count);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("bob", users[1].Name);
        Assert.Equal("carol", users[2].Name);
    }

    [Fact]
    public void QueryStringSingleOrDefault_WithNoMatch_ReturnsNull()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 999;

        // Act
        var user = connection.QueryStringSingleOrDefault<User>(
            $"SELECT id, name, email FROM users WHERE id = {userId}");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void ExecuteStringScalar_ReturnsValue()
    {
        // Arrange
        using var connection = CreateDatabase();

        // Act
        var count = connection.ExecuteStringScalar<long>(
            $"SELECT COUNT(*) FROM users WHERE email IS NOT NULL");

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void QueryString_WithNullColumn_MapsCorrectly()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 2;

        // Act
        var user = connection.QueryStringSingle<User>(
            $"SELECT id, name, email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal("bob", user.Name);
        Assert.Null(user.Email);
    }

    [Fact]
    public void ExecuteString_InsertAndQueryRoundTrip()
    {
        // Arrange
        using var connection = CreateDatabase();
        var id = 99;
        var name = "dave";
        string? email = null;

        connection.ExecuteString(
            $"INSERT INTO users (id, name, email) VALUES ({id}, {name}, {email})");

        // Act
        var user = connection.QueryStringSingle<User>(
            $"SELECT id, name, email FROM users WHERE id = {id}");

        // Assert
        Assert.Equal("dave", user.Name);
        Assert.Null(user.Email);
    }

    // --- Multi-row insert ---

    private record InsertUser(int Id, string Name, string? Email) : ISqliteRow
    {
        public SqliteFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [Fact]
    public void ExecuteString_WhenInsertingMultipleRowsWithISqliteRow_InsertsAllRows()
    {
        // Arrange
        using var connection = CreateDatabase();

        InsertUser[] users =
        [
            new(50, "eve", "eve@example.com"),
            new(51, "frank", null),
            new(52, "grace", "grace@example.com"),
        ];

        // Act
        connection.ExecuteString(
            $"INSERT INTO users (id, name, email) VALUES {Sqlite.InsertRows(users)}");

        // Assert
        var minId = 50;
        var maxId = 52;
        var inserted = connection.QueryString<User>(
            $"SELECT id, name, email FROM users WHERE id >= {minId} AND id <= {maxId} ORDER BY id").ToList();
        Assert.Equal(3, inserted.Count);
        Assert.Equal("eve", inserted[0].Name);
        Assert.Equal("frank", inserted[1].Name);
        Assert.Null(inserted[1].Email);
        Assert.Equal("grace", inserted[2].Name);
    }
}
