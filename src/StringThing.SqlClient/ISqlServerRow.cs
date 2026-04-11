namespace StringThing.SqlClient;

/// <summary>
/// Defines how a type maps to a SQL row for use with <see cref="SqlServerSql.InsertRows{T}"/>.
/// </summary>
public interface ISqlServerRow
{
    /// <summary>
    /// Returns the row values as a parenthesized, comma-separated SQL fragment.
    /// Example: <c>$"({Id}, {Name}, {Email})"</c>
    /// </summary>
    SqlServerFragment RowValues { get; }
}
