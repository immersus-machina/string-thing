namespace StringThing.Npgsql;

/// <summary>
/// Defines how a type maps to a SQL row for use with <see cref="PgSql.InsertRows{T}"/>.
/// </summary>
public interface IPostgresRow
{
    /// <summary>
    /// Returns the row values as a parenthesized, comma-separated SQL fragment.
    /// Example: <c>$"({Id}, {Name}, {Email})"</c>
    /// </summary>
    PostgresFragment RowValues { get; }
}
