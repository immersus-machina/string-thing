using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace StringThing.Analyzers.Tests;

public class UnmappableQueryTypeAnalyzerTests
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private const string StubTypes = """
        namespace StringThing.Aot
        {
            public interface IStringThingRow<TSelf> { }
        }

        namespace StringThing.Fake
        {
            public class FakeConnection { }

            public static class FakeResultExtensions
            {
                public static System.Collections.Generic.List<T> QueryString<T>(this FakeConnection connection) => null!;
                public static T QueryStringSingle<T>(this FakeConnection connection) => default!;
            }
        }

        namespace Other
        {
            public static class OtherExtensions
            {
                public static System.Collections.Generic.List<T> QueryString<T>(this StringThing.Fake.FakeConnection connection) => null!;
            }
        }

        namespace TestApp
        {
            public class PlainType { }

            public class RowType : StringThing.Aot.IStringThingRow<RowType> { }
        }
        """;

    private static CSharpAnalyzerTest<UnmappableQueryTypeAnalyzer, DefaultVerifier> CreateTest(
        string testCode,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<UnmappableQueryTypeAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }

    private static string Program(string body) => StubTypes + $$"""

        namespace TestApp
        {
            using StringThing.Fake;

            class Program
            {
                static void Run(FakeConnection connection)
                {
                    {{body}}
                }
            }
        }
        """;

    [Fact]
    public async Task QueryString_WithScalarInt_NoDiagnostic()
    {
        await CreateTest(Program("_ = connection.QueryString<int>();")).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryString_WithScalarString_NoDiagnostic()
    {
        await CreateTest(Program("_ = connection.QueryString<string>();")).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryString_WithNullableValueScalar_NoDiagnostic()
    {
        await CreateTest(Program("_ = connection.QueryString<int?>();")).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryString_WithByteArray_NoDiagnostic()
    {
        await CreateTest(Program("_ = connection.QueryString<byte[]>();")).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryString_WithRowType_NoDiagnostic()
    {
        await CreateTest(Program("_ = connection.QueryString<RowType>();")).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryStringSingle_WithRowType_NoDiagnostic()
    {
        await CreateTest(Program("_ = connection.QueryStringSingle<RowType>();")).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryString_WithUnsupportedType_ReportsDiagnostic()
    {
        await CreateTest(
            Program("_ = connection.QueryString<{|#0:PlainType|}>();"),
            new DiagnosticResult(UnmappableQueryTypeAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithLocation(0)).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryStringSingle_WithUnsupportedType_ReportsDiagnostic()
    {
        await CreateTest(
            Program("_ = connection.QueryStringSingle<{|#0:PlainType|}>();"),
            new DiagnosticResult(UnmappableQueryTypeAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithLocation(0)).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryString_WithTypeParameter_NoDiagnostic()
    {
        var testCode = StubTypes + """

            namespace TestApp
            {
                using StringThing.Fake;

                class Program
                {
                    static System.Collections.Generic.List<T> Passthrough<T>(FakeConnection connection)
                        => connection.QueryString<T>();
                }
            }
            """;

        await CreateTest(testCode).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryString_OnTypeOutsideStringThingNamespace_NoDiagnostic()
    {
        var testCode = StubTypes + """

            namespace TestApp
            {
                class Program
                {
                    static void Run(StringThing.Fake.FakeConnection connection)
                    {
                        _ = Other.OtherExtensions.QueryString<PlainType>(connection);
                    }
                }
            }
            """;

        await CreateTest(testCode).RunAsync(CancellationToken);
    }
}
