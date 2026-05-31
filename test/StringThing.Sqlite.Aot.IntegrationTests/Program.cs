using System.Data.Common;
using Microsoft.Data.Sqlite;
using StringThing.Aot;
using StringThing.Sqlite;

[StringThingRow]
public partial record AotUser(long Id, string Name, string? Email);

public sealed class AotHandRolledStatus : IStringThingRow<AotHandRolledStatus>
{
    public long Id { get; init; }
    public string Status { get; init; } = "";

    private static readonly string[] _columns = ["id", "email"];
    public static ReadOnlySpan<string> ColumnBindingOrder => _columns;

    public static AotHandRolledStatus Read(DbDataReader reader, ReadOnlySpan<int> ordinals) => new()
    {
        Id = reader.GetInt64(ordinals[0]),
        Status = reader.IsDBNull(ordinals[1]) ? "no-email" : "has-email",
    };
}

public static class Program
{
    public static int Main()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText =
                "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL, email TEXT);" +
                "INSERT INTO users (id, name, email) VALUES (1, 'alice', 'alice@example.com'), (2, 'bob', NULL);";
            create.ExecuteNonQuery();
        }

        var count = connection.QueryStringSingle<long>($"SELECT COUNT(*) FROM users");
        var names = connection.QueryString<string>($"SELECT name FROM users ORDER BY id");
        var missing = connection.QueryStringSingle<long?>($"SELECT CAST(NULL AS INTEGER)");
        var generatedRow = connection.QueryStringSingle<AotUser>(
            $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = 1");
        var handRolledRow = connection.QueryStringSingle<AotHandRolledStatus>(
            $"SELECT id, email FROM users WHERE id = 2");

        var throwsOnNull = false;
        try
        {
            connection.QueryStringSingle<long>($"SELECT CAST(NULL AS INTEGER)");
        }
        catch
        {
            throwsOnNull = true;
        }

        var passed =
            count == 2 &&
            names is ["alice", "bob"] &&
            missing is null &&
            generatedRow.Name == "alice" &&
            handRolledRow.Status == "no-email" &&
            throwsOnNull;

        Console.WriteLine($"count={count}");
        Console.WriteLine($"names=[{string.Join(", ", names)}]");
        Console.WriteLine($"missing={(missing is null ? "null" : missing.Value.ToString())}");
        Console.WriteLine($"generatedRow={generatedRow.Name}");
        Console.WriteLine($"handRolledRow={handRolledRow.Status}");
        Console.WriteLine($"nonNullableThrowsOnNull={throwsOnNull}");
        Console.WriteLine(passed ? "AOT OK" : "AOT FAIL");
        return passed ? 0 : 1;
    }
}
