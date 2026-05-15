using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using StringThing.Aot;
using StringThing.Core;
using StringThing.UnsafeSql;

using Xunit;

namespace StringThing.SqlClient.IntegrationTests;

[StringThingRow]
public partial record SqlServerRowUser(int Id, string Name, string? Email);

[StringThingRow]
public partial record SqlServerRowUserWithColumn(
    [property: Column("id")] int UserId,
    [property: Column("name")] string FullName,
    [property: Column("email")] string? EmailAddress);

[StringThingRow]
public partial class SqlServerRowUserMutable
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class SqlServerHandRolledUserStatus : IStringThingRow<SqlServerHandRolledUserStatus>
{
    public int Id { get; init; }
    public string Status { get; init; } = "";

    private static readonly string[] _columns = ["id", "email"];
    public static ReadOnlySpan<string> ColumnBindingOrder => _columns;

    public static SqlServerHandRolledUserStatus Read(DbDataReader reader, ReadOnlySpan<int> ordinals) => new()
    {
        Id = reader.GetInt32(ordinals[0]),
        Status = reader.IsDBNull(ordinals[1]) ? "no-email" : "has-email",
    };
}

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

    // --- AOT-generated row mapping ([StringThingRow]) ---

    [Fact]
    public async Task QueryStringSingleAsync_WithGeneratedRecord_MapsRow()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<SqlServerRowUser>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public async Task QueryStringAsync_WithGeneratedRecord_MapsMultipleRows()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var maxId = 3;

        // Act
        var users = await connection.QueryStringAsync<SqlServerRowUser>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id <= {maxId} ORDER BY id",
            CancellationToken);

        // Assert
        Assert.Equal(3, users.Count);
        Assert.Null(users[1].Email);
    }

    [Fact]
    public async Task QueryStringAsync_WithMutableClass_PopulatesSetters()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 3;

        // Act
        var user = await connection.QueryStringSingleAsync<SqlServerRowUserMutable>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("carol", user.Name);
    }

    [Fact]
    public async Task QueryStringAsync_WithColumnAttribute_HonorsOverride()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<SqlServerRowUserWithColumn>(
            $"SELECT id, name, email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal(1, user.UserId);
        Assert.Equal("alice", user.FullName);
    }

    // --- Hand-rolled IStringThingRow<T> (Model C — escape hatch when the generator can't be used) ---

    [Fact]
    public async Task QueryStringSingleAsync_WithHandRolledRow_DerivesStatusFromEmail()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<SqlServerHandRolledUserStatus>(
            $"SELECT id, email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("has-email", user.Status);
    }

    [Fact]
    public async Task QueryStringSingleAsync_WithHandRolledRow_DerivesStatusFromNullEmail()
    {
        // Arrange
        await using var connection = new SqlConnection(sqlServer.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 2;

        // Act
        var user = await connection.QueryStringSingleAsync<SqlServerHandRolledUserStatus>(
            $"SELECT id, email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal(2, user.Id);
        Assert.Equal("no-email", user.Status);
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
