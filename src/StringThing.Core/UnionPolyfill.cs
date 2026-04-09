// Required for C# 15 union types in .NET 11 Preview 2.
// These types ship in the runtime from Preview 3 onwards — remove this file then.

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class UnionAttribute : Attribute;

    public interface IUnion
    {
        object? Value { get; }
    }
}
