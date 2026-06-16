module StringThing.FSharp.Sqlite.Analyzers.Tests.Tests

open System
open System.IO
open System.Reflection
open FSharp.Analyzers.SDK
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open StringThing.FSharp.Sqlite.Analyzers.SupportedParameterTypeAnalyzer
open Xunit

let private checker = FSharpChecker.Create(keepAssemblyContents = true)

let private isManagedAssembly (path: string) : bool =
    try
        use stream = File.OpenRead(path)
        use peReader = new System.Reflection.PortableExecutable.PEReader(stream)
        if not peReader.HasMetadata then false
        else
            let reader = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(peReader)
            reader.IsAssembly
    with _ -> false

let private referencedDllPaths () : string list =
    let appDir =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) |> Option.ofObj
        |> Option.defaultValue AppContext.BaseDirectory
    let runtimeDir = Path.GetDirectoryName(typeof<obj>.Assembly.Location)
    let runtimeDlls =
        if Directory.Exists runtimeDir then
            Directory.GetFiles(runtimeDir, "*.dll")
            |> Array.filter isManagedAssembly
            |> Array.toList
        else []
    let appDlls = Directory.GetFiles(appDir, "*.dll") |> Array.filter isManagedAssembly |> Array.toList
    appDlls @ runtimeDlls
    |> List.distinctBy (fun p -> Path.GetFileName(p))

let private buildOptions (sourcePath: string) : FSharpProjectOptions =
    let refs = referencedDllPaths () |> List.map (fun p -> "-r:" + p)
    { ProjectFileName = "AnalyzerTestFixture.fsproj"
      ProjectId = None
      SourceFiles = [| sourcePath |]
      OtherOptions =
        [|
            yield "--target:library"
            yield "--targetprofile:netcore"
            yield "--noframework"
            yield "--simpleresolution"
            yield "--nowarn:3391"
            yield! refs
        |]
      ReferencedProjects = [||]
      IsIncompleteTypeCheckEnvironment = false
      UseScriptResolutionRules = false
      LoadTime = DateTime(2026, 1, 1)
      UnresolvedReferences = None
      OriginalLoadReferences = []
      Stamp = None }

let private runAnalyzer (source: string) : Message list =
    let path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".fs")
    File.WriteAllText(path, source)
    try
        let options = buildOptions path
        let parseResults, checkAnswer =
            checker.ParseAndCheckFileInProject(path, 0, SourceText.ofString source, options)
            |> Async.RunSynchronously
        match checkAnswer with
        | FSharpCheckFileAnswer.Aborted ->
            let parseErrors =
                parseResults.Diagnostics
                |> Array.map (fun d -> sprintf "  parse %d:%d %s" d.StartLine d.StartColumn d.Message)
                |> String.concat "\n"
            failwithf "Type check aborted. Parse diagnostics:\n%s" parseErrors
        | FSharpCheckFileAnswer.Succeeded checkResults ->
            let errors =
                checkResults.Diagnostics
                |> Array.filter (fun d -> d.Severity = FSharp.Compiler.Diagnostics.FSharpDiagnosticSeverity.Error)
            if errors.Length > 0 then
                let report =
                    errors
                    |> Array.map (fun d -> sprintf "  %d:%d %s" d.StartLine d.StartColumn d.Message)
                    |> String.concat "\n"
                failwithf "Fixture source has compile errors:\n%s" report
            let context : CliContext =
                { FileName = path
                  SourceText = SourceText.ofString source
                  ParseFileResults = parseResults
                  CheckFileResults = checkResults
                  CheckProjectResults = Unchecked.defaultof<_>
                  TypedTree = checkResults.ImplementationFile }
            supportedParameterTypeAnalyzer context |> Async.RunSynchronously
    finally
        File.Delete path

let private fixturePreamble = """module TestFixture
#nowarn "3391"
open System
open Microsoft.Data.Sqlite
open StringThing.FSharp
open StringThing.FSharp.Sqlite

type User = { Id: int64; Name: string }

let userRow : RowReader<User> =
    row {
        let! id = Row.int64 "id"
        and! name = Row.string "name"
        return { Id = id; Name = name }
    }

let connection : SqliteConnection = Unchecked.defaultof<_>
"""

let private codes (messages: Message list) =
    messages |> List.map (fun m -> m.Code) |> List.sort

// -- ST-FS-003: outer-argument provenance --

[<Fact>]
let ``ST-FS-003: inline $"" at call site is allowed`` () =
    let source = fixturePreamble + """
let test () =
    let id = 1L
    let _ = connection.QueryStringSingle userRow $"SELECT id, name FROM users WHERE id = {id}"
    ()
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``ST-FS-003: factored Sqlite.statement is allowed`` () =
    let source = fixturePreamble + """
let test () =
    let id = 1L
    let stmt = Sqlite.statement $"SELECT id, name FROM users WHERE id = {id}"
    let _ = connection.QueryStringSingle userRow stmt
    ()
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``ST-FS-003: factored Sqlite.statement with annotation is allowed`` () =
    let source = fixturePreamble + """
let test () =
    let id = 1L
    let stmt : SqliteStatement = Sqlite.statement $"SELECT id, name FROM users WHERE id = {id}"
    let _ = connection.QueryStringSingle userRow stmt
    ()
"""
    let result = runAnalyzer source
    let details =
        result
        |> List.map (fun m -> sprintf "%s @ %A: %s" m.Code m.Range m.Message)
        |> String.concat "\n"
    Assert.True(List.isEmpty result, sprintf "expected no diagnostics but got:\n%s" details)

[<Fact>]
let ``ST-FS-003: helper function returning SqlStatement is allowed`` () =
    let source = fixturePreamble + """
let buildQuery (id: int64) : SqliteStatement =
    Sqlite.statement $"SELECT id, name FROM users WHERE id = {id}"
let test () =
    let _ = connection.QueryStringSingle userRow (buildQuery 1L)
    ()
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``ST-FS-003: helper function returning FormattableString is rejected`` () =
    let source = fixturePreamble + """
let buildRaw (id: int64) : FormattableString =
    $"SELECT id, name FROM users WHERE id = {id}"
let test () =
    let _ = connection.QueryStringSingle userRow (buildRaw 1L)
    ()
"""
    Assert.Equal<string list>(["ST-FS-003"], codes (runAnalyzer source))

[<Fact>]
let ``ST-FS-003: parameter typed SqlStatement is allowed`` () =
    let source = fixturePreamble + """
let runIt (stmt: SqliteStatement) =
    let _ = connection.QueryStringSingle userRow stmt
    ()
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``ST-FS-003: parameter typed FormattableString is rejected`` () =
    let source = fixturePreamble + """
let runIt (stmt: FormattableString) =
    let _ = connection.QueryStringSingle userRow stmt
    ()
"""
    Assert.Equal<string list>(["ST-FS-003"], codes (runAnalyzer source))

[<Fact>]
let ``ST-FS-003: Sqlite.statement wrapping an untraceable FormattableString is rejected`` () =
    let source = fixturePreamble + """
let opaqueRaw () : FormattableString = $"SELECT id FROM users"
let test () =
    let raw = opaqueRaw ()
    let stmt = Sqlite.statement raw
    let _ = connection.QueryStringSingle userRow stmt
    ()
"""
    // `raw` comes from a helper returning FormattableString. Even when wrapped by
    // Sqlite.statement, the analyzer should refuse to vouch for it.
    let result = codes (runAnalyzer source)
    Assert.Contains("ST-FS-003", result)

[<Fact>]
let ``ST-FS-003: Sqlite.statement wrapping a traceable inline $"" is allowed`` () =
    let source = fixturePreamble + """
let test () =
    let raw : FormattableString = $"SELECT id FROM users"
    let stmt = Sqlite.statement raw
    let _ = connection.QueryStringSingle userRow stmt
    ()
"""
    // `raw` is bound to an inline $"" in this file — the chain is fully traceable.
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``supported scalar parameters produce no diagnostic`` () =
    let source = fixturePreamble + """
let test () =
    let id = 1L
    let name = "alice"
    connection.ExecuteString $"INSERT INTO users VALUES ({id}, {name})" |> ignore
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``unsupported hole type produces ST-FS-001`` () =
    let source = fixturePreamble + """
let test () =
    let address = System.Net.IPAddress.Loopback
    connection.ExecuteString $"INSERT INTO requests VALUES ({address})" |> ignore
"""
    Assert.Equal<string list>(["ST-FS-001"], codes (runAnalyzer source))

[<Fact>]
let ``inline nested $"" is allowed and analyzed recursively`` () =
    let source = fixturePreamble + """
let test () =
    let minId = 1L
    let inner : FormattableString = $"id > {minId}"
    let _ = connection.QueryString userRow $"SELECT id, name FROM users WHERE {inner}"
    ()
"""
    // The user assigned the inner $"" to a FormattableString variable — this is the
    // case the analyzer must reject (ST-FS-002), because a raw FormattableString variable
    // can't be analyzed at the splice. The recommended pattern is Sqlite.fragment.
    Assert.Equal<string list>(["ST-FS-002"], codes (runAnalyzer source))

[<Fact>]
let ``SqlFragment value spliced via op_Implicit is allowed`` () =
    let source = fixturePreamble + """
let test () =
    let minId = 1L
    let filter = Sqlite.fragment $"id > {minId}"
    let _ = connection.QueryString userRow $"SELECT id, name FROM users WHERE {filter}"
    ()
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``Sqlite.unsafe returns obj which is allowed in a hole`` () =
    let source = fixturePreamble + """
let test () =
    let table = Sqlite.unsafe "users"
    let id = 1L
    let _ = connection.QueryStringSingle userRow $"SELECT id, name FROM {table} WHERE id = {id}"
    ()
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``Sqlite.inList returns obj which is allowed in a hole`` () =
    let source = fixturePreamble + """
let test () =
    let ids = [ 1L; 2L; 3L ]
    let _ = connection.QueryString userRow $"SELECT id, name FROM users WHERE id IN {Sqlite.inList ids}"
    ()
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``option-wrapped supported type is allowed`` () =
    let source = fixturePreamble + """
let test () =
    let email : string option = Some "alice@example.com"
    connection.ExecuteString $"INSERT INTO users VALUES (1, 'alice', {email})" |> ignore
"""
    Assert.Equal<string list>([], codes (runAnalyzer source))

[<Fact>]
let ``option of unsupported type is flagged`` () =
    let source = fixturePreamble + """
let test () =
    let addr : System.Net.IPAddress option = Some System.Net.IPAddress.Loopback
    connection.ExecuteString $"INSERT INTO requests VALUES ({addr})" |> ignore
"""
    Assert.Equal<string list>(["ST-FS-001"], codes (runAnalyzer source))
