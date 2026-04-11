namespace StringThing.MySql;

/// <summary>
/// Defines how a type maps to a SQL row for use with <see cref="MySql.InsertRows{T}"/>.
/// </summary>
public interface IMySqlRow
{
    /// <summary>
    /// Returns the row values as a parenthesized, comma-separated SQL fragment.
    /// Example: <c>$"({Id}, {Name}, {Email})"</c>
    /// </summary>
    MySqlFragment RowValues { get; }
}
