using MySqlConnector;
using StringThing.UnsafeSql;
using Xunit;

namespace StringThing.MySql.IntegrationTests;

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
}
