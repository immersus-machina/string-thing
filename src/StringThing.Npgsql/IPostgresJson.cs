namespace StringThing.Npgsql;

/// <summary>
/// Defines how a type serializes to JSON for storage in a Postgres jsonb column.
/// </summary>
public interface IPostgresJson
{
    string ToJson();
}
