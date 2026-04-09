using Xunit;

namespace StringThing.Npgsql.IntegrationTests;

public class SqlStatementIntegrationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await using var command = postgres.DataSource.CreateCommand("""
            CREATE TABLE IF NOT EXISTS users (
                id integer PRIMARY KEY,
                name text NOT NULL,
                email text,
                tags text[] NOT NULL DEFAULT '{}',
                created_at timestamptz NOT NULL DEFAULT now()
            )
            """);
        await command.ExecuteNonQueryAsync(CancellationToken);

        await using var insert = postgres.DataSource.CreateCommand("""
            INSERT INTO users (id, name, email, tags, created_at) VALUES
                (1, 'alice', 'alice@example.com', '{admin,user}', '2026-01-15T10:00:00Z'),
                (2, 'bob', NULL, '{user}', '2026-02-20T14:30:00Z'),
                (3, 'carol', 'carol@example.com', '{admin}', '2026-03-10T09:00:00Z')
            ON CONFLICT (id) DO NOTHING
            """);
        await insert.ExecuteNonQueryAsync(CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenQueryingWithScalarParameter_ReturnsMatchingRow()
    {
        // Arrange
        var userId = 1;

        // Act
        PostgresSql stmt = $"SELECT name FROM users WHERE id = {userId}";
        await using var command = stmt.ToCommand(postgres.DataSource);
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public async Task WhenQueryingWithNullParameter_MatchesNullColumn()
    {
        // Arrange
        string? email = null;

        // Act
        PostgresSql stmt = $"SELECT name FROM users WHERE email IS NOT DISTINCT FROM {email}";
        await using var command = stmt.ToCommand(postgres.DataSource);
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("bob", result);
    }

    [Fact]
    public async Task WhenQueryingWithArrayParameter_ReturnsMatchingRows()
    {
        // Arrange
        var ids = new[] { 1, 3 };

        // Act
        PostgresSql stmt = $"SELECT name FROM users WHERE id = ANY({ids}) ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));

        // Assert
        Assert.Equal(["alice", "carol"], names);
    }

    [Fact]
    public async Task WhenQueryingWithStringArrayContainment_ReturnsMatchingRows()
    {
        // Arrange
        IReadOnlyList<string> requiredTags = ["admin"];

        // Act
        PostgresSql stmt = $"SELECT name FROM users WHERE tags @> {requiredTags} ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));

        // Assert
        Assert.Equal(["alice", "carol"], names);
    }

    [Fact]
    public async Task WhenQueryingWithMultipleTypedParameters_ReturnsFilteredResults()
    {
        // Arrange
        var minId = 1;
        var cutoff = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        PostgresSql stmt = $"SELECT name FROM users WHERE id > {minId} AND created_at < {cutoff} ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));

        // Assert
        Assert.Equal(["bob"], names);
    }

    [Fact]
    public async Task WhenQueryingWithDeduplicatedParameter_ProducesCorrectSql()
    {
        // Arrange
        var searchTerm = "alice";

        // Act
        PostgresSql stmt = $"SELECT name FROM users WHERE name = {searchTerm} OR email LIKE {searchTerm} || '%'";
        await using var command = stmt.ToCommand(postgres.DataSource);
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
        SqlFragment filter = $"id > {minId} AND tags @> ARRAY[{status}]";

        // Act
        PostgresSql stmt = $"SELECT name FROM users WHERE {filter} ORDER BY id";
        await using var command = stmt.ToCommand(postgres.DataSource);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            names.Add(reader.GetString(0));

        // Assert
        Assert.Equal(["carol"], names);
    }

    [Fact]
    public async Task WhenQueryingWithUnsafeSql_SplicesCorrectly()
    {
        // Arrange
        var tableName = Sql.Unsafe("users");
        var userId = 2;

        // Act
        PostgresSql stmt = $"SELECT name FROM {tableName} WHERE id = {userId}";
        await using var command = stmt.ToCommand(postgres.DataSource);
        var result = await command.ExecuteScalarAsync(CancellationToken);

        // Assert
        Assert.Equal("bob", result);
    }
}
