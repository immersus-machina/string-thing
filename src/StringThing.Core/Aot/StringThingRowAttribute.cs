namespace StringThing.Aot;

/// <summary>
/// Marks a type for AOT-friendly row materialization. The source generator emits an
/// <see cref="IStringThingRow{TSelf}"/> implementation as a partial member of the type.
/// The type must be declared <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class StringThingRowAttribute : Attribute
{
}
