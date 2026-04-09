namespace StringThing;

public readonly struct UnsafeSql
{
    public string RawText { get; }
    internal UnsafeSql(string rawText) { RawText = rawText; }
}
