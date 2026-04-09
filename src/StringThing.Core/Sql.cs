namespace StringThing;

public static class Sql
{
    public static UnsafeSql Unsafe(string rawSql) => new(rawSql);
}
