using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using MySqlConnector;
using StringThing.Aot;
using StringThing.UnsafeSql;
using Xunit;

namespace StringThing.MySql.IntegrationTests;

[StringThingRow]
public partial record MySqlRowUser(int Id, string Name, string? Email);

[StringThingRow]
public partial record MySqlRowUserWithColumn(
    [property: Column("id")] int UserId,
    [property: Column("name")] string FullName,
    [property: Column("email")] string? EmailAddress);

[StringThingRow]
public partial class MySqlRowUserMutable
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class MySqlHandRolledUserStatus : IStringThingRow<MySqlHandRolledUserStatus>
{
    public int Id { get; init; }
    public string Status { get; init; } = "";

    private static readonly string[] _columns = ["id", "email"];
    public static ReadOnlySpan<string> ColumnBindingOrder => _columns;

    public static MySqlHandRolledUserStatus Read(DbDataReader reader, ReadOnlySpan<int> ordinals) => new()
    {
        Id = reader.GetInt32(ordinals[0]),
        Status = reader.IsDBNull(ordinals[1]) ? "no-email" : "has-email",
    };
}

public class MySqlIntegrationTests(MySqlFixture mySql) : IClassFixture<MySqlFixture>, IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        await using var createTable = connection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE IF NOT EXISTS users (
                id int PRIMARY KEY,
                name varchar(100) NOT NULL,
                email varchar(200) NULL,
                active boolean NOT NULL DEFAULT true
            )
            """;
        await createTable.ExecuteNonQueryAsync(CancellationToken);

        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT IGNORE INTO users (id, name, email, active) VALUES
                (1, 'alice', 'alice@example.com', true),
                (2, 'bob', NULL, true),
                (3, 'carol', 'carol@example.com', false)
            """;
        await insert.ExecuteNonQueryAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenQueryingWithScalarParameter_ReturnsMatchingRow()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;
        MySql stmt = $"SELECT name FROM users WHERE id = {userId}";
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 2;
        MySql stmt = $"SELECT email FROM users WHERE id = {userId}";
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var minId = 1;
        var maxId = 3;
        var active = true;
        MySql stmt = $"SELECT name FROM users WHERE id > {minId} AND id <= {maxId} AND active = {active} ORDER BY id";
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var searchTerm = "alice";
        MySql stmt = $"SELECT name FROM users WHERE name = {searchTerm} OR email LIKE CONCAT({searchTerm}, '%')";
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var id = 99;
        var name = "dave";
        string? email = null;
        MySql insertStmt = $"INSERT IGNORE INTO users (id, name, email) VALUES ({id}, {name}, {email})";
        await using var insertCmd = insertStmt.ToCommand(connection);
        await insertCmd.ExecuteNonQueryAsync(CancellationToken);

        MySql selectStmt = $"SELECT email FROM users WHERE id = {id}";
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var tableName = Sql.Unsafe("users");
        var userId = 3;
        MySql stmt = $"SELECT name FROM {tableName} WHERE id = {userId}";
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var minId = 0;
        var maxId = 3;
        var active = true;
        MySqlFragment filter = $"id > {minId} AND id <= {maxId} AND active = {active}";
        MySql stmt = $"SELECT name FROM users WHERE {filter} ORDER BY id";
        await using var command = stmt.ToCommand(connection);

        // Act
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        // Assert
        var names = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));
        Assert.Equal(["alice", "bob"], names);
    }

    // --- Multi-row insert with IMySqlRow ---

    private record InsertUser(int Id, string Name, string? Email) : IMySqlRow
    {
        public MySqlFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [Fact]
    public async Task WhenInsertingMultipleRowsWithIMySqlRow_InsertsAllRows()
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
        MySql stmt = $"INSERT INTO users (id, name, email) VALUES {MySql.InsertRows(users)}";
        await using var command = stmt.ToCommand(connection);

        // Act
        await command.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        var minId = 50;
        var maxId = 52;
        MySql verifyStmt = $"SELECT name, email FROM users WHERE id >= {minId} AND id <= {maxId} ORDER BY id";
        await using var verifyCmd = verifyStmt.ToCommand(connection);
        await using var reader = await verifyCmd.ExecuteReaderAsync(CancellationToken);
        var results = new List<(string Name, string? Email)>();
        while (await reader.ReadAsync(CancellationToken))
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        Assert.Equal(3, results.Count);
        Assert.Equal("eve", results[0].Name);
        Assert.Equal("frank", results[1].Name);
        Assert.Null(results[1].Email);
        Assert.Equal("grace", results[2].Name);
    }

    // --- AOT-generated row mapping ([StringThingRow]) ---

    [Fact]
    public async Task QueryStringSingleAsync_WithGeneratedRecord_MapsRow()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<MySqlRowUser>(
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var maxId = 3;

        // Act
        var users = await connection.QueryStringAsync<MySqlRowUser>(
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 3;

        // Act
        var user = await connection.QueryStringSingleAsync<MySqlRowUserMutable>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("carol", user.Name);
    }

    [Fact]
    public async Task QueryStringAsync_WithColumnAttribute_HonorsOverride()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<MySqlRowUserWithColumn>(
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<MySqlHandRolledUserStatus>(
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
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 2;

        // Act
        var user = await connection.QueryStringSingleAsync<MySqlHandRolledUserStatus>(
            $"SELECT id, email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal(2, user.Id);
        Assert.Equal("no-email", user.Status);
    }

    // --- Scalar result mapping (language primitives, not row types) ---

    [Fact]
    public async Task QueryStringSingleAsync_WithScalarLong_ReadsColumnZero()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        // Act
        var count = await connection.QueryStringSingleAsync<long>(
            $"SELECT COUNT(*) FROM users WHERE id <= 3", CancellationToken);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task QueryStringAsync_WithScalarString_ReadsEachRowAsValue()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        // Act
        var names = await connection.QueryStringAsync<string>(
            $"SELECT name FROM users WHERE id <= 3 ORDER BY id", CancellationToken);

        // Assert
        Assert.Equal(["alice", "bob", "carol"], names);
    }

    [Fact]
    public async Task QueryStringSingleAsync_WithNullableScalar_ReturnsNullForDbNull()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 2;

        // Act
        var email = await connection.QueryStringSingleAsync<string?>(
            $"SELECT email FROM users WHERE id = {userId}", CancellationToken);

        // Assert
        Assert.Null(email);
    }

    [Fact]
    public async Task QueryStringSingleAsync_WithNullableValueScalar_ReturnsValue()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var userId = 1;

        // Act
        var id = await connection.QueryStringSingleAsync<int?>(
            $"SELECT id FROM users WHERE id = {userId}", CancellationToken);

        // Assert
        Assert.Equal(1, id);
    }

    [Fact]
    public async Task QueryStringSingleAsync_WithNullableValueScalar_ReturnsNullForDbNull()
    {
        // Arrange
        await using var connection = new MySqlConnection(mySql.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        // Act
        var value = await connection.QueryStringSingleAsync<int?>(
            $"SELECT CAST(NULL AS SIGNED)", CancellationToken);

        // Assert
        Assert.Null(value);
    }
}
