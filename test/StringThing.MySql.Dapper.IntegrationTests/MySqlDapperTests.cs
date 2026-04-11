using MySqlConnector;
using Xunit;

namespace StringThing.MySql.Dapper.IntegrationTests;

public class MySqlDapperTests(MySqlFixture mySql) : IClassFixture<MySqlFixture>, IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        await connection.ExecuteStringAsync(
            $"""
            CREATE TABLE IF NOT EXISTS dapper_users (
                id int PRIMARY KEY,
                name varchar(100) NOT NULL,
                email varchar(200) NULL
            )
            """,
            CancellationToken);

        await connection.ExecuteStringAsync(
            $"""
            INSERT IGNORE INTO dapper_users (id, name, email) VALUES
                (1, 'alice', 'alice@example.com'),
                (2, 'bob', NULL),
                (3, 'carol', 'carol@example.com')
            """,
            CancellationToken);
    }

    private record User(int Id, string Name, string? Email);

    [Fact]
    public async Task QueryStringSingleAsync_ReturnsMappedObject()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
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
    public async Task QueryStringSingleOrDefaultAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 999;

        // Act
        var user = await connection.QueryStringSingleOrDefaultAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task ExecuteStringScalarAsync_ReturnsValue()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var maxId = 3;

        // Act
        var count = await connection.ExecuteStringScalarAsync<int>(
            $"SELECT COUNT(*) FROM dapper_users WHERE email IS NOT NULL AND id <= {maxId}",
            CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task QueryStringAsync_WithNullColumn_MapsCorrectly()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 2;

        // Act
        var user = await connection.QueryStringSingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("bob", user.Name);
        Assert.Null(user.Email);
    }

    [Fact]
    public async Task ExecuteStringAsync_InsertAndQueryRoundTrip()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var id = 99;
        var name = "dave";
        string? email = null;

        await connection.ExecuteStringAsync(
            $"INSERT IGNORE INTO dapper_users (id, name, email) VALUES ({id}, {name}, {email})",
            CancellationToken);

        // Act
        var user = await connection.QueryStringSingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {id}",
            CancellationToken);

        // Assert
        Assert.Equal("dave", user.Name);
        Assert.Null(user.Email);
    }

    // --- Multi-row insert with IMySqlRow ---

    private record InsertUser(int Id, string Name, string? Email) : IMySqlRow
    {
        public MySqlFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [Fact]
    public async Task ExecuteStringAsync_WhenInsertingMultipleRowsWithIMySqlRow_InsertsAllRows()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        InsertUser[] users =
        [
            new(50, "eve", "eve@example.com"),
            new(51, "frank", null),
            new(52, "grace", "grace@example.com"),
        ];

        // Act
        await connection.ExecuteStringAsync(
            $"INSERT INTO dapper_users (id, name, email) VALUES {MySql.InsertRows(users)}",
            CancellationToken);

        // Assert
        var inserted = (await connection.QueryStringAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id >= {50} AND id <= {52} ORDER BY id",
            CancellationToken)).ToList();
        Assert.Equal(3, inserted.Count);
        Assert.Equal("eve", inserted[0].Name);
        Assert.Equal("frank", inserted[1].Name);
        Assert.Null(inserted[1].Email);
        Assert.Equal("grace", inserted[2].Name);
    }
}
