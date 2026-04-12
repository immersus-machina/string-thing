using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace StringThing.Analyzers.Tests;

public class DuplicateHandlerPerLineAnalyzerTests
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private const string StubTypes = """
        using System.Runtime.CompilerServices;

        namespace StringThing.Core
        {
            public abstract class SqlStatement<TNamer, TParameter>
                where TNamer : class
                where TParameter : class
            {
                public SqlStatement(
                    int literalLength,
                    int formattedCount,
                    [CallerFilePath] string filePath = "",
                    [CallerLineNumber] int lineNumber = 0) { }

                public void AppendLiteral(string text) { }
                public void AppendFormatted<T>(T value) { }
            }

            public class SqlFragment<TParameter> where TParameter : class
            {
                public SqlFragment(int literalLength, int formattedCount) { }
                public void AppendLiteral(string text) { }
                public void AppendFormatted<T>(T value) { }
            }
        }

        namespace TestApp
        {
            public class TestNamer { }
            public class TestParameter { }

            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public sealed class TestSql : StringThing.Core.SqlStatement<TestNamer, TestParameter>
            {
                public TestSql(
                    int literalLength,
                    int formattedCount,
                    [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
                    [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
                    : base(literalLength, formattedCount, filePath, lineNumber) { }
            }

            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public sealed class TestFragment : StringThing.Core.SqlFragment<TestParameter>
            {
                public TestFragment(int literalLength, int formattedCount)
                    : base(literalLength, formattedCount) { }
            }
        }
        """;

    private static CSharpAnalyzerTest<DuplicateHandlerPerLineAnalyzer, DefaultVerifier> CreateTest(
        string testCode,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<DuplicateHandlerPerLineAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }

    [Fact]
    public async Task WhenSingleHandlerPerLine_NoDiagnostic()
    {
        var testCode = StubTypes + """

            class Program
            {
                static void Main()
                {
                    var x = 1;
                    TestApp.TestSql a = $"SELECT {x}";
                    TestApp.TestSql b = $"SELECT {x}";
                }
            }
            """;

        await CreateTest(testCode).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenTernaryOnSeparateLines_NoDiagnostic()
    {
        var testCode = StubTypes + """

            class Program
            {
                static void Main()
                {
                    var x = 1;
                    var y = 2;
                    bool condition = true;
                    TestApp.TestSql stmt = condition
                        ? (TestApp.TestSql)$"SELECT {x}"
                        : (TestApp.TestSql)$"SELECT {y}";
                }
            }
            """;

        await CreateTest(testCode).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenFragmentHandlersOnSameLine_NoDiagnostic()
    {
        var testCode = StubTypes + """

            class Program
            {
                static void Main()
                {
                    var x = 1; var y = 2;
                    TestApp.TestFragment a = $"{x}"; TestApp.TestFragment b = $"{y}";
                }
            }
            """;

        await CreateTest(testCode).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenMixedStatementAndFragmentOnSameLine_NoDiagnostic()
    {
        var testCode = StubTypes + """

            class Program
            {
                static void Main()
                {
                    var x = 1; var y = 2;
                    TestApp.TestSql a = $"{x}"; TestApp.TestFragment b = $"{y}";
                }
            }
            """;

        await CreateTest(testCode).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenTwoStatementsOnSameLine_ReportsDiagnostic()
    {
        var testCode = StubTypes + """

            class Program
            {
                static void Main()
                {
                    var x = 1; var y = 2;
                    TestApp.TestSql a = $"{x}"; TestApp.TestSql b = {|#0:$"{y}"|};
                }
            }
            """;

        await CreateTest(testCode,
            new DiagnosticResult(DuplicateHandlerPerLineAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithLocation(0)).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenTernaryOnSingleLine_ReportsDiagnostic()
    {
        var testCode = StubTypes + """

            class Program
            {
                static void Main()
                {
                    var x = 1; var y = 2;
                    bool c = true;
                    TestApp.TestSql stmt = c ? (TestApp.TestSql)$"{x}" : (TestApp.TestSql){|#0:$"{y}"|};
                }
            }
            """;

        await CreateTest(testCode,
            new DiagnosticResult(DuplicateHandlerPerLineAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithLocation(0)).RunAsync(CancellationToken);
    }

    [Fact]
    public async Task WhenProjectDoesNotReferenceStringThing_NoDiagnostic()
    {
        var testCode = """
            class Program
            {
                static void Main()
                {
                    var a = $"hello"; var b = $"world";
                }
            }
            """;

        await CreateTest(testCode).RunAsync(CancellationToken);
    }
}
