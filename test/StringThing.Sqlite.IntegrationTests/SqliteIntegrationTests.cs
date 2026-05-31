using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using StringThing.Aot;
using StringThing.UnsafeSql;
using Xunit;

namespace StringThing.Sqlite.IntegrationTests;

[StringThingRow]
public partial record RowUser(long Id, string Name, string? Email);

[StringThingRow]
public partial class RowUserMutable
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

[StringThingRow]
public partial record RowUserWithColumn(
    [property: Column("id")] long UserId,
    [property: Column("name")] string FullName,
    [property: Column("email")] string? EmailAddress);

public sealed class HandRolledUserStatus : IStringThingRow<HandRolledUserStatus>
{
    public long Id { get; init; }
    public string Status { get; init; } = "";

    private static readonly string[] _columns = ["id", "email"];
    public static ReadOnlySpan<string> ColumnBindingOrder => _columns;

    public static HandRolledUserStatus Read(DbDataReader reader, ReadOnlySpan<int> ordinals) => new()
    {
        Id = reader.GetInt64(ordinals[0]),
        Status = reader.IsDBNull(ordinals[1]) ? "no-email" : "has-email",
    };
}

public class SqliteIntegrationTests
{
    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var createTable = connection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT,
                active INTEGER NOT NULL DEFAULT 1
            )
            """;
        createTable.ExecuteNonQuery();

        using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO users (id, name, email, active) VALUES
                (1, 'alice', 'alice@example.com', 1),
                (2, 'bob', NULL, 1),
                (3, 'carol', 'carol@example.com', 0)
            """;
        insert.ExecuteNonQuery();

        return connection;
    }

    [Fact]
    public void WhenQueryingWithScalarParameter_ReturnsMatchingRow()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;
        SqliteSql stmt = $"SELECT name FROM users WHERE id = {userId}";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public void WhenQueryingWithNullParameter_MatchesNullColumn()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 2;
        SqliteSql stmt = $"SELECT email FROM users WHERE id = {userId}";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public void WhenQueryingWithMultipleParameters_FiltersCorrectly()
    {
        // Arrange
        using var connection = CreateDatabase();
        var minId = 1;
        var active = true;
        SqliteSql stmt = $"SELECT name FROM users WHERE id > {minId} AND active = {active} ORDER BY id";
        using var command = stmt.ToCommand(connection);

        // Act
        using var reader = command.ExecuteReader();

        // Assert
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        Assert.Equal(["bob"], names);
    }

    [Fact]
    public void WhenQueryingWithDeduplicatedParameter_ProducesCorrectResult()
    {
        // Arrange
        using var connection = CreateDatabase();
        var searchTerm = "alice";
        SqliteSql stmt = $"SELECT name FROM users WHERE name = {searchTerm} OR email LIKE {searchTerm} || '%'";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public void WhenInsertingWithNullParameter_InsertsNull()
    {
        // Arrange
        using var connection = CreateDatabase();
        var id = 99;
        var name = "dave";
        string? email = null;
        SqliteSql insertStmt = $"INSERT INTO users (id, name, email) VALUES ({id}, {name}, {email})";
        using var insertCmd = insertStmt.ToCommand(connection);
        insertCmd.ExecuteNonQuery();

        SqliteSql selectStmt = $"SELECT email FROM users WHERE id = {id}";
        using var selectCmd = selectStmt.ToCommand(connection);

        // Act
        var result = selectCmd.ExecuteScalar();

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public void WhenUsingUnsafeSql_SplicesCorrectly()
    {
        // Arrange
        using var connection = CreateDatabase();
        var tableName = Sql.Unsafe("users");
        var userId = 3;
        SqliteSql stmt = $"SELECT name FROM {tableName} WHERE id = {userId}";
        using var command = stmt.ToCommand(connection);

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.Equal("carol", result);
    }

    [Fact]
    public void WhenUsingFragment_ComposesAndExecutesCorrectly()
    {
        // Arrange
        using var connection = CreateDatabase();
        var minId = 0;
        var active = true;
        SqliteFragment filter = $"id > {minId} AND active = {active}";
        SqliteSql stmt = $"SELECT name FROM users WHERE {filter} ORDER BY id";
        using var command = stmt.ToCommand(connection);

        // Act
        using var reader = command.ExecuteReader();

        // Assert
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        Assert.Equal(["alice", "bob"], names);
    }

    // --- Multi-row insert ---

    private record InsertUser(int Id, string Name, string? Email) : ISqliteRow
    {
        public SqliteFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [Fact]
    public void WhenInsertingMultipleRowsWithISqliteRow_InsertsAllRows()
    {
        // Arrange
        using var connection = CreateDatabase();

        InsertUser[] users =
        [
            new(50, "eve", "eve@example.com"),
            new(51, "frank", null),
            new(52, "grace", "grace@example.com"),
        ];
        SqliteSql stmt = $"INSERT INTO users (id, name, email) VALUES {SqliteSql.InsertRows(users)}";
        using var command = stmt.ToCommand(connection);

        // Act
        command.ExecuteNonQuery();

        // Assert
        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT name FROM users WHERE id >= 50 AND id <= 52 ORDER BY id";
        using var reader = verify.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        Assert.Equal(["eve", "frank", "grace"], names);
    }

    // --- AOT-generated row mapping ([StringThingRow]) ---

    [Fact]
    public void QueryStringSingle_WithGeneratedRecord_MapsRow()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<RowUser>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public void QueryString_WithGeneratedRecord_MapsMultipleRows()
    {
        // Arrange
        using var connection = CreateDatabase();
        var maxId = 3;

        // Act
        var users = connection.QueryString<RowUser>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id <= {maxId} ORDER BY id");

        // Assert
        Assert.Equal(3, users.Count);
        Assert.Equal("alice", users[0].Name);
        Assert.Null(users[1].Email);
    }

    [Fact]
    public void QueryString_WithShuffledSelectOrder_ResolvesByName()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<RowUser>(
            $"SELECT email AS Email, id AS Id, name AS Name FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public void QueryString_WithMutableClass_PopulatesSetters()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 3;

        // Act
        var user = connection.QueryStringSingle<RowUserMutable>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(3, user.Id);
        Assert.Equal("carol", user.Name);
        Assert.Equal("carol@example.com", user.Email);
    }

    [Fact]
    public void QueryString_WithColumnAttribute_HonorsOverride()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<RowUserWithColumn>(
            $"SELECT id, name, email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, user.UserId);
        Assert.Equal("alice", user.FullName);
        Assert.Equal("alice@example.com", user.EmailAddress);
    }

    [Fact]
    public void QueryStringSingleOrDefault_WithNoMatch_ReturnsNull()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 999;

        // Act
        var user = connection.QueryStringSingleOrDefault<RowUser>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

        // Assert
        Assert.Null(user);
    }

    // --- Scalar result mapping (language primitives, not row types) ---

    [Fact]
    public void QueryStringSingle_WithScalarLong_ReadsColumnZero()
    {
        // Arrange
        using var connection = CreateDatabase();

        // Act
        var count = connection.QueryStringSingle<long>($"SELECT COUNT(*) FROM users");

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void QueryString_WithScalarString_ReadsEachRowAsValue()
    {
        // Arrange
        using var connection = CreateDatabase();

        // Act
        var names = connection.QueryString<string>($"SELECT name FROM users ORDER BY id");

        // Assert
        Assert.Equal(["alice", "bob", "carol"], names);
    }

    [Fact]
    public void QueryStringSingle_WithNullableScalar_ReturnsNullForDbNull()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 2;

        // Act
        var email = connection.QueryStringSingle<string?>(
            $"SELECT email FROM users WHERE id = {userId}");

        // Assert
        Assert.Null(email);
    }

    [Fact]
    public void QueryStringSingle_WithNullableValueScalar_ReturnsValue()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var active = connection.QueryStringSingle<int?>(
            $"SELECT active FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, active);
    }

    [Fact]
    public void QueryStringSingle_WithNullableValueScalar_ReturnsNullForDbNull()
    {
        // Act
        using var connection = CreateDatabase();
        var value = connection.QueryStringSingle<int?>($"SELECT CAST(NULL AS INTEGER)");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void QueryStringSingle_WithNonNullableValueScalar_ThrowsOnDbNull()
    {
        // Arrange
        using var connection = CreateDatabase();

        // Act + Assert
        Assert.ThrowsAny<Exception>(() =>
            connection.QueryStringSingle<int>($"SELECT CAST(NULL AS INTEGER)"));
    }

    // --- Hand-rolled IStringThingRow<T> (Model C — escape hatch when the generator can't be used) ---

    [Fact]
    public void QueryStringSingle_WithHandRolledRow_DerivesStatusFromEmail()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 1;

        // Act
        var user = connection.QueryStringSingle<HandRolledUserStatus>(
            $"SELECT id, email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("has-email", user.Status);
    }

    [Fact]
    public void QueryStringSingle_WithHandRolledRow_DerivesStatusFromNullEmail()
    {
        // Arrange
        using var connection = CreateDatabase();
        var userId = 2;

        // Act
        var user = connection.QueryStringSingle<HandRolledUserStatus>(
            $"SELECT id, email FROM users WHERE id = {userId}");

        // Assert
        Assert.Equal(2, user.Id);
        Assert.Equal("no-email", user.Status);
    }
}
