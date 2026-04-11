namespace StringThing.UnsafeSql;

public static class Sql
{
    public static UnsafeSqlFragment Unsafe(string rawSql) => new(rawSql);
}
