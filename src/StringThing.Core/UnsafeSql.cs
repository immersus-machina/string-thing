namespace StringThing.UnsafeSql;

public readonly struct UnsafeSqlFragment
{
    internal string RawText { get; }
    internal UnsafeSqlFragment(string rawText) { RawText = rawText; }
}
