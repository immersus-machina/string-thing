using Microsoft.Data.SqlClient;
using Xunit;

namespace StringThing.SqlClient.Dapper.IntegrationTests;

public class SqlServerSqlDapperTests(SqlServerFixture sqlServer) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        await using var createTable = new SqlCommand("""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dapper_users')
            CREATE TABLE dapper_users (
                id int PRIMARY KEY,
                name nvarchar(100) NOT NULL,
                email nvarchar(200) NULL
            )
            """, connection);
        await createTable.ExecuteNonQueryAsync(CancellationToken);

        await using var insert = new SqlCommand("""
            IF NOT EXISTS (SELECT 1 FROM dapper_users WHERE id = 1)
            BEGIN
                INSERT INTO dapper_users (id, name, email) VALUES
                    (1, 'alice', 'alice@example.com'),
                    (2, 'bob', NULL),
                    (3, 'carol', 'carol@example.com')
            END
            """, connection);
        await insert.ExecuteNonQueryAsync(CancellationToken);
    }

    private record User(int Id, string Name, string? Email);

    [Fact]
    public async Task QuerySingleAsync_ReturnsMappedObject()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QuerySingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public async Task QueryAsync_ReturnsMultipleMappedObjects()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var maxId = 3;

        // Act
        var users = (await connection.QueryAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id <= {maxId} ORDER BY id",
            CancellationToken)).ToList();

        // Assert
        Assert.Equal(3, users.Count);
        Assert.Equal("alice", users[0].Name);
        Assert.Equal("bob", users[1].Name);
        Assert.Equal("carol", users[2].Name);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 999;

        // Act
        var user = await connection.QuerySingleOrDefaultAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ReturnsValue()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        // Act
        var count = await connection.ExecuteScalarAsync<int>(
            $"SELECT {Sql.Unsafe("COUNT(*)")} FROM dapper_users WHERE email IS NOT NULL",
            CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task QueryAsync_WithNullColumn_MapsCorrectly()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 2;

        // Act
        var user = await connection.QuerySingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("bob", user.Name);
        Assert.Null(user.Email);
    }

    [Fact]
    public async Task ExecuteAsync_InsertAndQueryRoundTrip()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var id = 99;
        var name = "dave";
        string? email = null;

        await connection.ExecuteAsync(
            $"INSERT INTO dapper_users (id, name, email) VALUES ({id}, {name}, {email})",
            CancellationToken);

        // Act
        var user = await connection.QuerySingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {id}",
            CancellationToken);

        // Assert
        Assert.Equal("dave", user.Name);
        Assert.Null(user.Email);
    }
}
