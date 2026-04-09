using Microsoft.Data.SqlClient;

namespace StringThing.SqlClient;

internal readonly record struct SqlElement
{
    public enum Tag : byte
    {
        None = 0,
        Literal,
        Parameter,
    }

    private readonly Tag _tag;
    private readonly SqlParameter? _parameter;
    private readonly string? _referenceSlot;

    private SqlElement(Tag tag, SqlParameter? parameter, string? text)
    {
        _tag = tag;
        _parameter = parameter;
        _referenceSlot = text;
    }

    public static SqlElement Literal(string literalText) =>
        new(Tag.Literal, null, literalText);

    public static SqlElement Param(SqlParameter parameter, string? capturedExpression) =>
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

    public bool TryGetParameter(out SqlParameter parameter, out string? capturedExpression)
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
