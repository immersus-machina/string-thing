using System.Data;
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

        await connection.ExecuteStringAsync(
            $"""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dapper_users')
            CREATE TABLE dapper_users (
                id int PRIMARY KEY,
                name nvarchar(100) NOT NULL,
                email nvarchar(200) NULL
            );
            IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'UserTableType')
            CREATE TYPE dbo.UserTableType AS TABLE (
                id int,
                name nvarchar(100),
                email nvarchar(200) NULL
            );
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tvp_users')
            CREATE TABLE tvp_users (
                id int PRIMARY KEY,
                name nvarchar(100) NOT NULL,
                email nvarchar(200) NULL
            );
            """,
            CancellationToken);

        await connection.ExecuteStringAsync(
            $"""
            IF NOT EXISTS (SELECT 1 FROM dapper_users WHERE id = 1)
            BEGIN
                INSERT INTO dapper_users (id, name, email) VALUES
                    (1, 'alice', 'alice@example.com'),
                    (2, 'bob', NULL),
                    (3, 'carol', 'carol@example.com')
            END
            """,
            CancellationToken);
    }

    private record User(int Id, string Name, string? Email);

    [Fact]
    public async Task QueryStringSingleAsync_ReturnsMappedObject()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
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
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
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
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
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
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        // Act
        var maxId = 3;
        var count = await connection.ExecuteStringScalarAsync<int>(
            $"SELECT {Sql.Unsafe("COUNT(*)")} FROM dapper_users WHERE email IS NOT NULL AND id <= {maxId}",
            CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task QueryStringAsync_WithNullColumn_MapsCorrectly()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
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
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var id = 99;
        var name = "dave";
        string? email = null;

        await connection.ExecuteStringAsync(
            $"INSERT INTO dapper_users (id, name, email) VALUES ({id}, {name}, {email})",
            CancellationToken);

        // Act
        var user = await connection.QueryStringSingleAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id = {id}",
            CancellationToken);

        // Assert
        Assert.Equal("dave", user.Name);
        Assert.Null(user.Email);
    }

    // --- Multi insert with ISqlServerRow ---

    private record InsertUser(int Id, string Name, string? Email) : ISqlServerRow
    {
        public SqlServerFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [Fact]
    public async Task ExecuteStringAsync_WhenInsertingMultipleRowsWithISqlServerRow_InsertsAllRows()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        InsertUser[] users =
        [
            new(50, "eve", "eve@example.com"),
            new(51, "frank", null),
            new(52, "grace", "grace@example.com"),
        ];

        // Act
        await connection.ExecuteStringAsync(
            $"INSERT INTO dapper_users (id, name, email) VALUES {SqlServerSql.InsertRows(users)}",
            CancellationToken);

        // Assert
        var inserted = (await connection.QueryStringAsync<User>(
            $"SELECT id, name, email FROM dapper_users WHERE id >= {50} AND id <= {52} ORDER BY id",
            CancellationToken)).ToList();
        Assert.Equal(3, inserted.Count);
        Assert.Equal("eve", inserted[0].Name);
        Assert.Equal("eve@example.com", inserted[0].Email);
        Assert.Equal("frank", inserted[1].Name);
        Assert.Null(inserted[1].Email);
        Assert.Equal("grace", inserted[2].Name);
    }

    // --- Batch insert with Table-Valued Parameter ---

    [Fact]
    public async Task ExecuteStringAsync_WhenInsertingWithTableValuedParameter_InsertsAllRows()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        using var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("email", typeof(string));
        table.Rows.Add(70, "heidi", "heidi@example.com");
        table.Rows.Add(71, "ivan", DBNull.Value);
        table.Rows.Add(72, "judy", "judy@example.com");

        // Act
        await connection.ExecuteStringAsync(
            $"INSERT INTO tvp_users (id, name, email) SELECT id, name, email FROM {SqlServerSql.Table(table, "dbo.UserTableType")}",
            CancellationToken);

        // Assert
        var minId = 70;
        var maxId = 72;
        var results = (await connection.QueryStringAsync<User>(
            $"SELECT id, name, email FROM tvp_users WHERE id >= {minId} AND id <= {maxId} ORDER BY id",
            CancellationToken)).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal("heidi", results[0].Name);
        Assert.Equal("ivan", results[1].Name);
        Assert.Null(results[1].Email);
        Assert.Equal("judy", results[2].Name);
    }
}
