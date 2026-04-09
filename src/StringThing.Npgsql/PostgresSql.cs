using System.Runtime.CompilerServices;

namespace StringThing.Npgsql;

[InterpolatedStringHandler]
public sealed class PostgresSql : SqlStatement<PostgresParameterNamer>
{
    public PostgresSql(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }
}
