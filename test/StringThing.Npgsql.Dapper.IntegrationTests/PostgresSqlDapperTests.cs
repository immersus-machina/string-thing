using StringThing.Core;
using StringThing.UnsafeSql;

using Xunit;

namespace StringThing.Npgsql.Dapper.IntegrationTests;

public class PostgresSqlDapperTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteStringAsync(
            $"""
            CREATE TABLE IF NOT EXISTS dapper_users (
                id integer PRIMARY KEY,
                name text NOT NULL,
                email text
            )
            """,
            CancellationToken);

        await connection.ExecuteStringAsync(
            $"""
            INSERT INTO dapper_users (id, name, email) VALUES
                (1, 'alice', 'alice@example.com'),
                (2, 'bob', NULL),
                (3, 'carol', 'carol@example.com')
            ON CONFLICT (id) DO NOTHING
            """,
            CancellationToken);
    }

    private record User(int Id, string Name, string? Email);

    [Fact]
    public async Task QueryStringSingleAsync_ReturnsMappedObject()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public async Task QueryStringAsync_ReturnsMultipleMappedObjects()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);

        var maxId = 3;

        // Act
        var users = (await connection.QueryStringAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id <= {maxId} ORDER BY id",
            CancellationToken)).ToList();

        // Assert
        Assert.Equal(3, users.Count);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("bob", users[1].Name);
        Assert.Equal("carol", users[2].Name);
    }

    [Fact]
    public async Task QueryStringAsync_WithMultipleParameters_FiltersCorrectly()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var minId = 1;
        var excludeName = "carol";

        var maxId = 3;

        // Act
        var users = (await connection.QueryStringAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id > {minId} AND id <= {maxId} AND name != {excludeName} ORDER BY id",
            CancellationToken)).ToList();

        // Assert
        Assert.Single(users);
        Assert.Equal("bob", users[0].Name);
    }

    [Fact]
    public async Task QueryStringSingleOrDefaultAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 999;

        // Act
        var user = await connection.QueryStringSingleOrDefaultAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task QueryStringFirstAsync_ReturnsFirstMatch()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var minId = 0;

        // Act
        var user = await connection.QueryStringFirstAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id > {minId} ORDER BY id",
            CancellationToken);

        // Assert
        Assert.Equal("alice", user.Name);
    }

    [Fact]
    public async Task ExecuteStringScalarAsync_ReturnsValue()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);

        // Act
        var count = await connection.ExecuteStringScalarAsync<int>(
            $"SELECT {Sql.Unsafe("COUNT(*)")} FROM dapper_users WHERE email IS NOT NULL",
            CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExecuteStringAsync_InsertAndQueryRoundTrip()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var id = 99;
        var name = "dave";
        string? email = null;

        await connection.ExecuteStringAsync(
            $"INSERT INTO dapper_users (id, name, email) VALUES ({id}, {name}, {email}) ON CONFLICT (id) DO NOTHING",
            CancellationToken);

        // Act
        var user = await connection.QueryStringSingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {id}",
            CancellationToken);

        // Assert
        Assert.Equal("dave", user.Name);
        Assert.Null(user.Email);
    }

    [Fact]
    public async Task QueryStringAsync_WithNullColumn_MapsCorrectly()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 2;

        // Act
        var user = await connection.QueryStringSingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("bob", user.Name);
        Assert.Null(user.Email);
    }
}
