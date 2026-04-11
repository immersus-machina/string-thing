namespace StringThing.MySql;

/// <summary>
/// Defines how a type serializes to JSON for storage in a MySQL JSON column.
/// </summary>
public interface IMySqlJson
{
    string ToJson();
}
