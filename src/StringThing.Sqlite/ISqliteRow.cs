namespace StringThing.Sqlite;

/// <summary>
/// Defines how a type maps to a SQL row for use with <see cref="Sqlite.InsertRows{T}"/>.
/// </summary>
public interface ISqliteRow
{
    /// <summary>
    /// Returns the row values as a parenthesized, comma-separated SQL fragment.
    /// Example: <c>$"({Id}, {Name}, {Email})"</c>
    /// </summary>
    SqliteFragment RowValues { get; }
}
