namespace StringThing;

internal readonly record struct SqlElement<TParameter> where TParameter : class
{
    public enum Tag : byte
    {
        None = 0,
        Literal,
        Parameter,
    }

    private readonly Tag _tag;
    private readonly TParameter? _parameter;
    private readonly string? _referenceSlot;

    private SqlElement(Tag tag, TParameter? parameter, string? text)
    {
        _tag = tag;
        _parameter = parameter;
        _referenceSlot = text;
    }

    public static SqlElement<TParameter> Literal(string literalText) =>
        new(Tag.Literal, null, literalText);

    public static SqlElement<TParameter> Param(TParameter parameter, string? capturedExpression) =>
        new(Tag.Parameter, parameter, capturedExpression);

    public Tag Kind => _tag;

    public bool TryGetLiteral(out string literalText)
    {
        if (_tag == Tag.Literal)
        {
            literalText = _referenceSlot!;
            return true;
        }
        literalText = string.Empty;
        return false;
    }

    public bool TryGetParameter(out TParameter parameter, out string? capturedExpression)
    {
        if (_tag == Tag.Parameter)
        {
            parameter = _parameter!;
            capturedExpression = _referenceSlot;
            return true;
        }
        parameter = null!;
        capturedExpression = null;
        return false;
    }
}
