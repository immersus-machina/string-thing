using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using StringThing.Aot;
using StringThing.UnsafeSql;

using Xunit;

namespace StringThing.Npgsql.IntegrationTests;

[StringThingRow]
public partial record PostgresRowUser(int Id, string Name, string? Email);

[StringThingRow]
public partial record PostgresRowUserWithColumn(
    [property: Column("id")] int UserId,
    [property: Column("name")] string FullName,
    [property: Column("email")] string? EmailAddress);

[StringThingRow]
public partial class PostgresRowUserMutable
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class PostgresHandRolledUserStatus : IStringThingRow<PostgresHandRolledUserStatus>
{
    public int Id { get; init; }
    public string Status { get; init; } = "";

    private static readonly string[] _columns = ["id", "email"];
    public static ReadOnlySpan<string> ColumnBindingOrder => _columns;

    public static PostgresHandRolledUserStatus Read(DbDataReader reader, ReadOnlySpan<int> ordinals) => new()
    {
        Id = reader.GetInt32(ordinals[0]),
        Status = reader.IsDBNull(ordinals[1]) ? "no-email" : "has-email",
    };
}

public class PostgresSqlIntegrationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var createUsers = postgres.DataSource.CreateCommand("""
            CREATE TABLE IF NOT EXISTS users (
                id integer PRIMARY KEY,
                name text NOT NULL,
                email text,
                tags text[] NOT NULL DEFAULT '{}',
                created_at timestamptz NOT NULL DEFAULT now()
            );
            CREATE TABLE IF NOT EXISTS pgrow_test (
                id integer PRIMARY KEY,
                name text NOT NULL,
                email text
            );
            CREATE TABLE IF NOT EXISTS pgrow_single_test (
                id integer PRIMARY KEY,
                name text NOT NULL,
                email text
            );
            CREATE TABLE IF NOT EXISTS unnest_test (
                id integer PRIMARY KEY,
                name text NOT NULL,
                email text
            );
            """);
        await createUsers.ExecuteNonQueryAsync(CancellationToken);

        await using var insert = postgres.DataSource.CreateCommand("""
            INSERT INTO users (id, name, email, tags, created_at) VALUES
                (1, 'alice', 'alice@example.com', '{admin,user}', '2026-01-15T10:00:00Z'),
                (2, 'bob', NULL, '{user}', '2026-02-20T14:30:00Z'),
                (3, 'carol', 'carol@example.com', '{admin}', '2026-03-10T09:00:00Z')
            ON CONFLICT (id) DO NOTHING
            """);
        await insert.ExecuteNonQueryAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenQueryingWithScalarParameter_ReturnsMatchingRow()
    {
        // Arrange
        var userId = 1;

        PostgresSql stmt = $"SELECT name FROM users WHERE id = {userId}";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public async Task WhenQueryingWithNullParameter_MatchesNullColumn()
    {
        // Arrange
        string? email = null;

        PostgresSql stmt = $"SELECT name FROM users WHERE email IS NOT DISTINCT FROM {email}";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("bob", result);
    }

    [Fact]
    public async Task WhenQueryingWithArrayParameter_ReturnsMatchingRows()
    {
        // Arrange
        var ids = new[] { 1, 3 };

        PostgresSql stmt = $"SELECT name FROM users WHERE id = ANY({ids}) ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        // Assert
        var names = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));
        Assert.Equal(["alice", "carol"], names);
    }

    [Fact]
    public async Task WhenQueryingWithStringArrayContainment_ReturnsMatchingRows()
    {
        // Arrange
        string[] requiredTags = ["admin"];

        PostgresSql stmt = $"SELECT name FROM users WHERE tags @> {requiredTags} ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        // Assert
        var names = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));
        Assert.Equal(["alice", "carol"], names);
    }

    [Fact]
    public async Task WhenQueryingWithMultipleTypedParameters_ReturnsFilteredResults()
    {
        // Arrange
        var minId = 1;
        var cutoff = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        PostgresSql stmt = $"SELECT name FROM users WHERE id > {minId} AND created_at < {cutoff} ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);

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
        var searchTerm = "alice";

        PostgresSql stmt = $"SELECT name FROM users WHERE name = {searchTerm} OR email LIKE {searchTerm} || '%'";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public async Task WhenQueryingWithFragment_ComposesAndExecutesCorrectly()
    {
        // Arrange
        var minId = 1;
        var status = "admin";
        PostgresFragment filter = $"id > {minId} AND tags @> ARRAY[{status}]";

        PostgresSql stmt = $"SELECT name FROM users WHERE {filter} ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        // Assert
        var names = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));
        Assert.Equal(["carol"], names);
    }

    [Fact]
    public async Task WhenQueryingWithUnsafeSql_SplicesCorrectly()
    {
        // Arrange
        var tableName = Sql.Unsafe("users");
        var userId = 2;

        PostgresSql stmt = $"SELECT name FROM {tableName} WHERE id = {userId}";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("bob", result);
    }

    // --- Multi insert with IPostgresRow ---

    private record InsertUser(int Id, string Name, string? Email) : IPostgresRow
    {
        public PostgresFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [Fact]
    public async Task WhenInsertingMultipleRowsWithIPostgresRow_InsertsAllRows()
    {
        // Arrange
        InsertUser[] users =
        [
            new(1, "alice", "alice@example.com"),
            new(2, "bob", null),
            new(3, "carol", "carol@example.com"),
        ];

        PostgresSql stmt = $"INSERT INTO pgrow_test (id, name, email) VALUES {PostgresSql.InsertRows(users)}";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await command.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        await using var verify = postgres.DataSource.CreateCommand("SELECT name, email FROM pgrow_test ORDER BY id");
        var results = new List<(string Name, string? Email)>();
        await using var reader = await verify.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        Assert.Equal(
            [("alice", "alice@example.com"), ("bob", null), ("carol", "carol@example.com")],
            results);
    }

    [Fact]
    public async Task WhenInsertingSingleRowWithIPostgresRow_Works()
    {
        // Arrange
        InsertUser[] users = [new(1, "alice", null)];

        PostgresSql stmt = $"INSERT INTO pgrow_single_test (id, name, email) VALUES {PostgresSql.InsertRows(users)}";
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await command.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        await using var verify = postgres.DataSource.CreateCommand("SELECT name FROM pgrow_single_test");
        var result = await verify.ExecuteScalarAsync(CancellationToken);
        Assert.Equal("alice", result);
    }

    // --- Batch insert with UNNEST ---

    [Fact]
    public async Task WhenInsertingWithUnnest_InsertsAllRows()
    {
        // Arrange
        var ids = new[] { 10, 11, 12 };
        var names = new[] { "alice", "bob", "carol" };
        var emails = new[] { "alice@example.com", "bob@example.com", "carol@example.com" };

        PostgresSql stmt = $"""
            INSERT INTO unnest_test (id, name, email)
            SELECT * FROM UNNEST({ids}, {names}, {emails})
            """;
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await command.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        await using var verify = postgres.DataSource.CreateCommand("SELECT name FROM unnest_test WHERE id BETWEEN 10 AND 12 ORDER BY id");
        var results = new List<string>();
        await using var reader = await verify.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            results.Add(reader.GetString(0));
        Assert.Equal(["alice", "bob", "carol"], results);
    }

    [Fact]
    public async Task WhenInsertingManyRowsWithUnnest_HandlesLargeBatch()
    {
        // Arrange
        const int rowCount = 200;
        var ids = Enumerable.Range(1000, rowCount).ToArray();
        var names = ids.Select(id => $"user_{id}").ToArray();
        var emails = ids.Select(id => $"user_{id}@example.com").ToArray();

        PostgresSql stmt = $"""
            INSERT INTO unnest_test (id, name, email)
            SELECT * FROM UNNEST({ids}, {names}, {emails})
            """;
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await command.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        await using var countCmd = postgres.DataSource.CreateCommand("SELECT COUNT(*) FROM unnest_test WHERE id >= 1000");
        var count = (long)(await countCmd.ExecuteScalarAsync(CancellationToken))!;
        Assert.Equal(rowCount, count);
    }

    [Fact]
    public async Task WhenInsertingWithUnnestAndNullableColumn_HandlesNulls()
    {
        // Arrange
        var ids = new[] { 100, 101, 102 };
        var names = new[] { "dave", "eve", "frank" };
        string?[] emails = ["dave@example.com", null, "frank@example.com"];

        PostgresSql stmt = $"""
            INSERT INTO unnest_test (id, name, email)
            SELECT * FROM UNNEST({ids}, {names}, {emails})
            """;
        await using var command = stmt.ToCommand(postgres.DataSource);

        // Act
        await command.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        await using var verify = postgres.DataSource.CreateCommand("SELECT email FROM unnest_test WHERE id BETWEEN 100 AND 102 ORDER BY id");
        var results = new List<string?>();
        await using var reader = await verify.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            results.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
        Assert.Equal(["dave@example.com", null, "frank@example.com"], results);
    }

    // --- AOT-generated row mapping ([StringThingRow]) ---

    [Fact]
    public async Task QueryStringSingleAsync_WithGeneratedRecord_MapsRow()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<PostgresRowUser>(
            $"SELECT id AS \"Id\", name AS \"Name\", email AS \"Email\" FROM users WHERE id = {userId}",
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
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var maxId = 3;

        // Act
        var users = await connection.QueryStringAsync<PostgresRowUser>(
            $"SELECT id AS \"Id\", name AS \"Name\", email AS \"Email\" FROM users WHERE id <= {maxId} ORDER BY id",
            CancellationToken);

        // Assert
        Assert.Equal(3, users.Count);
        Assert.Null(users[1].Email);
    }

    [Fact]
    public async Task QueryStringAsync_WithMutableClass_PopulatesSetters()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 3;

        // Act
        var user = await connection.QueryStringSingleAsync<PostgresRowUserMutable>(
            $"SELECT id AS \"Id\", name AS \"Name\", email AS \"Email\" FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal("carol", user.Name);
    }

    [Fact]
    public async Task QueryStringAsync_WithColumnAttribute_HonorsOverride()
    {
        // Arrange
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<PostgresRowUserWithColumn>(
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
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 1;

        // Act
        var user = await connection.QueryStringSingleAsync<PostgresHandRolledUserStatus>(
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
        await using var connection = await postgres.DataSource.OpenConnectionAsync(CancellationToken);
        var userId = 2;

        // Act
        var user = await connection.QueryStringSingleAsync<PostgresHandRolledUserStatus>(
            $"SELECT id, email FROM users WHERE id = {userId}",
            CancellationToken);

        // Assert
        Assert.Equal(2, user.Id);
        Assert.Equal("no-email", user.Status);
    }
}
