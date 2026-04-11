using Microsoft.Data.SqlClient;
using StringThing.Core;
using StringThing.UnsafeSql;

using Xunit;

namespace StringThing.SqlClient.IntegrationTests;

public class SqlServerSqlIntegrationTests(SqlServerFixture sqlServer) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        await using var createTable = new SqlCommand("""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
            CREATE TABLE users (
                id int PRIMARY KEY,
                name nvarchar(100) NOT NULL,
                email nvarchar(200) NULL,
                active bit NOT NULL DEFAULT 1
            )
            """, connection);
        await createTable.ExecuteNonQueryAsync(CancellationToken);

        await using var insert = new SqlCommand("""
            IF NOT EXISTS (SELECT 1 FROM users WHERE id = 1)
            BEGIN
                INSERT INTO users (id, name, email, active) VALUES
                    (1, 'alice', 'alice@example.com', 1),
                    (2, 'bob', NULL, 1),
                    (3, 'carol', 'carol@example.com', 0)
            END
            """, connection);
        await insert.ExecuteNonQueryAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenQueryingWithScalarParameter_ReturnsMatchingRow()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;
        SqlServerSql stmt = $"SELECT name FROM users WHERE id = {userId}";
        await using var command = stmt.ToCommand(connection);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public async Task WhenQueryingWithNullParameter_MatchesNullColumn()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 2;

        SqlServerSql stmt = $"SELECT email FROM users WHERE id = {userId}";
        await using var command = stmt.ToCommand(connection);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public async Task WhenQueryingWithMultipleParameters_FiltersCorrectly()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var minId = 1;
        var active = true;

        var maxId = 3;
        SqlServerSql stmt = $"SELECT name FROM users WHERE id > {minId} AND id <= {maxId} AND active = {active} ORDER BY id";
        await using var command = stmt.ToCommand(connection);

        // Act
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        // Assert
        var names = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));
        Assert.Equal(["bob"], names);
    }

    [Fact]
    public async Task WhenQueryingWithDeduplicatedParameter_ProducesCorrectSql()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var searchTerm = "alice";

        SqlServerSql stmt = $"SELECT name FROM users WHERE name = {searchTerm} OR email LIKE {searchTerm} + '%'";
        await using var command = stmt.ToCommand(connection);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public async Task WhenInsertingWithNullParameter_InsertsNull()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var id = 99;
        var name = "dave";
        string? email = null;

        SqlServerSql insertStmt = $"INSERT INTO users (id, name, email) VALUES ({id}, {name}, {email})";
        await using var insertCmd = insertStmt.ToCommand(connection);
        await insertCmd.ExecuteNonQueryAsync(CancellationToken);
        SqlServerSql selectStmt = $"SELECT email FROM users WHERE id = {id}";
        await using var selectCmd = selectStmt.ToCommand(connection);

        // Act
        var result = await selectCmd.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public async Task WhenUsingUnsafeSql_SplicesCorrectly()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var tableName = Sql.Unsafe("users");
        var userId = 3;

        SqlServerSql stmt = $"SELECT name FROM {tableName} WHERE id = {userId}";
        await using var command = stmt.ToCommand(connection);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("carol", result);
    }

    [Fact]
    public async Task WhenUsingFragment_ComposesAndExecutesCorrectly()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var minId = 0;
        var active = true;
        SqlServerFragment filter = $"id > {minId} AND active = {active}";

        var maxId = 3;
        SqlServerSql stmt = $"SELECT name FROM users WHERE {filter} AND id <= {maxId} ORDER BY id";
        await using var command = stmt.ToCommand(connection);

        // Act
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        // Assert
        var names = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));
        Assert.Equal(["alice", "bob"], names);
    }

    [Fact]
    public async Task WhenUsingIndexedParameterNamer_ProducesIndexedPlaceholders()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;
        var active = true;

        SqlServerStatement<IndexedParameterNamer> stmt = $"SELECT name FROM users WHERE id = {userId} AND active = {active}";
        await using var command = stmt.ToCommand(connection);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("alice", result);
    }
}
