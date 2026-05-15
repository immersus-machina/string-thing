using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.Sqlite;
using StringThing.Aot;
using StringThing.Sqlite.Aot;
using Xunit;

namespace StringThing.Sqlite.Aot.IntegrationTests;

[StringThingRow]
public partial record User(long Id, string Name, string? Email);

[StringThingRow]
public partial record UserWithColumnAttribute(
    [property: Column("id")] long UserId,
    [property: Column("name")] string FullName,
    [property: Column("email")] string? EmailAddress);

[StringThingRow]
public partial class UserMutable
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

public class SqliteAotTests
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

    [Fact]
    public void QueryStringSingle_WithRecord_ReturnsMappedObject()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<User>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, user.Id);
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
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id <= {maxId} ORDER BY id");

        // Assert
        Assert.Equal(3, users.Count);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("bob", users[1].Name);
        Assert.Equal("carol", users[2].Name);
    }

    [Fact]
    public void QueryString_WithNullColumn_MapsToNull()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 2;

        // Act
        var user = connection.QueryStringSingle<User>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal("bob", user.Name);
        Assert.Null(user.Email);
    }

    [Fact]
    public void QueryStringSingleOrDefault_WithNoMatch_ReturnsNull()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 999;

        // Act
        var user = connection.QueryStringSingleOrDefault<User>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void QueryString_WithShuffledSelectOrder_ResolvesByName()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<User>(
            $"SELECT email AS Email, id AS Id, name AS Name FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public void QueryString_WithExtraColumnInSelect_Ignored()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<User>(
            $"SELECT id AS Id, name AS Name, email AS Email, 'extra' AS unused FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal("alice", user.Name);
    }

    [Fact]
    public void QueryString_WithColumnAttribute_HonorsOverride()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<UserWithColumnAttribute>(
            $"SELECT id, name, email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, user.UserId);
        Assert.Equal("alice", user.FullName);
        Assert.Equal("alice@example.com", user.EmailAddress);
    }

    [Fact]
    public void QueryString_WithMutableClass_PopulatesSetters()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 3;

        // Act
        var user = connection.QueryStringSingle<UserMutable>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(3, user.Id);
        Assert.Equal("carol", user.Name);
        Assert.Equal("carol@example.com", user.Email);
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
    public async Task QueryStringAsync_ReturnsMultipleMappedObjects()
    {
        // Arrange
        await using var connection = CreateDatabase();
        var maxId = 3;

        // Act
        var users = await connection.QueryStringAsync<User>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id <= {maxId} ORDER BY id",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, users.Count);
    }
}
