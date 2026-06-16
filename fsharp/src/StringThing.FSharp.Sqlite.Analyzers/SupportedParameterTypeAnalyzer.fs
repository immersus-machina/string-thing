module StringThing.FSharp.Sqlite.Analyzers.SupportedParameterTypeAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open FSharp.Compiler.Symbols.FSharpExprPatterns
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text

let private supportedScalarFullNames =
    Set.ofList [
        "System.Boolean"
        "System.Int32"
        "System.Int64"
        "System.Double"
        "System.String"
        "System.Guid"
        "System.DateTime"
        "System.Object"
    ]

let private blessedWrapperFullNames =
    Set.ofList [
        "StringThing.FSharp.Sqlite.SqlStatement"
        "StringThing.FSharp.Sqlite.SqlFragment"
    ]

let private formattableStringFactoryFullName =
    "System.Runtime.CompilerServices.FormattableStringFactory"

let private formattableStringFullName = "System.FormattableString"

let private tryEntityFullName (entity: FSharpEntity) =
    try entity.TryFullName
    with _ -> None

let rec private stripAbbreviation (t: FSharpType) : FSharpType =
    if t.IsAbbreviation then stripAbbreviation t.AbbreviatedType else t

let private isOptionDefinition (entity: FSharpEntity) : bool =
    tryEntityFullName entity = Some "Microsoft.FSharp.Core.FSharpOption`1"

let private isByteArray (t: FSharpType) : bool =
    let stripped = stripAbbreviation t
    if not stripped.HasTypeDefinition then false
    else
        let definition = stripped.TypeDefinition
        let isArray =
            try definition.IsArrayType
            with _ -> false
        if not isArray then false
        elif stripped.GenericArguments.Count <> 1 then false
        else
            let element = stripAbbreviation stripped.GenericArguments.[0]
            element.HasTypeDefinition
            && tryEntityFullName element.TypeDefinition = Some "System.Byte"

let rec private isSupportedHoleType (t: FSharpType) : bool =
    let t = stripAbbreviation t
    if isByteArray t then
        true
    elif t.HasTypeDefinition && isOptionDefinition t.TypeDefinition then
        t.GenericArguments.Count = 1
        && isSupportedHoleType t.GenericArguments.[0]
    elif t.HasTypeDefinition then
        match tryEntityFullName t.TypeDefinition with
        | Some fullName ->
            supportedScalarFullNames.Contains fullName
            || blessedWrapperFullNames.Contains fullName
        | None -> false
    else
        false

let private isFormattableStringType (t: FSharpType) : bool =
    let stripped = stripAbbreviation t
    stripped.HasTypeDefinition
    && tryEntityFullName stripped.TypeDefinition = Some formattableStringFullName

let private displayName (t: FSharpType) : string =
    let stripped = stripAbbreviation t
    if isByteArray stripped then "byte[]"
    elif stripped.HasTypeDefinition then
        match tryEntityFullName stripped.TypeDefinition with
        | Some name -> name
        | None ->
            try stripped.TypeDefinition.DisplayName
            with _ -> stripped.Format(FSharpDisplayContext.Empty)
    else
        try stripped.Format(FSharpDisplayContext.Empty)
        with _ -> "<unknown>"

let private unsupportedHoleMessage (range: Range) (typeName: string) : Message =
    { Type = "StringThing.FSharp.UnsupportedParameterType"
      Message =
          sprintf
              "Unsupported StringThing parameter type: '%s'. Use Sqlite.unsafe for raw SQL, Sqlite.inList for IN clauses, or Sqlite.fragment to compose another SQL fragment."
              typeName
      Code = "ST-FS-001"
      Severity = Severity.Error
      Range = range
      Fixes = [] }

let private rejectedFormattableStringHoleMessage (range: Range) : Message =
    { Type = "StringThing.FSharp.RawFormattableStringHole"
      Message =
          "A FormattableString hole must be either an inline `$\"...\"` or a value of type SqlFragment (built via Sqlite.fragment). Raw FormattableString values cannot be spliced."
      Code = "ST-FS-002"
      Severity = Severity.Error
      Range = range
      Fixes = [] }

let private isFormattableStringFactoryCreate (mfv: FSharpMemberOrFunctionOrValue) : bool =
    if mfv.LogicalName <> "Create" then false
    else
        match mfv.DeclaringEntity with
        | Some entity -> tryEntityFullName entity = Some formattableStringFactoryFullName
        | None -> false

let private isOpImplicitFromBlessedWrapper (mfv: FSharpMemberOrFunctionOrValue) : bool =
    if mfv.LogicalName <> "op_Implicit" then false
    else
        match mfv.DeclaringEntity with
        | Some entity ->
            match tryEntityFullName entity with
            | Some name -> blessedWrapperFullNames.Contains name
            | None -> false
        | None -> false

let private isBoxOperator (mfv: FSharpMemberOrFunctionOrValue) : bool =
    if mfv.LogicalName <> "box" then false
    else
        match mfv.DeclaringEntity with
        | Some entity ->
            tryEntityFullName entity = Some "Microsoft.FSharp.Core.Operators"
        | None -> false

let private stripCoerce (expr: FSharpExpr) : FSharpExpr =
    match expr with
    | Coerce(_, inner) -> inner
    | Call(_, mfv, _, _, [ inner ]) when isBoxOperator mfv -> inner
    | _ -> expr

let private checkHole (boxed: FSharpExpr) (sink: ResizeArray<Message>) : unit =
    let inner = stripCoerce boxed
    let innerType = inner.Type
    if isSupportedHoleType innerType then
        ()
    elif isFormattableStringType innerType then
        match inner with
        | Call(_, mfv, _, _, _) when isFormattableStringFactoryCreate mfv -> ()
        | Call(_, mfv, _, _, _) when isOpImplicitFromBlessedWrapper mfv -> ()
        | _ ->
            sink.Add(rejectedFormattableStringHoleMessage inner.Range)
    else
        sink.Add(unsupportedHoleMessage inner.Range (displayName innerType))

let private analyseFormattableStringCreate (args: FSharpExpr list) (sink: ResizeArray<Message>) : unit =
    match args with
    | _ :: arrayExpr :: _ ->
        match arrayExpr with
        | NewArray(_, elements) ->
            for element in elements do
                checkHole element sink
        | _ -> ()
    | _ -> ()

// ---- ST-FS-003: outer-argument provenance check ----
//
// Operates on the untyped syntax tree (and the symbol table for non-local references) so
// that F# 9's implicit-conversion rewriting can't hide the original construction.
// At every call to a SqliteConnection target method (or to Sqlite.statement / Sqlite.fragment),
// the FormattableString-shaped argument must be a "blessed expression".

let private targetConnectionMethodNames =
    Set.ofList [
        "QueryStringSingle"
        "QueryStringSingleOrDefault"
        "QueryString"
        "ExecuteString"
        "ExecuteStringScalar"
    ]

let private factoryMethodNames = Set.ofList [ "statement"; "fragment" ]

let private rejectedProvenanceMessage (range: Range) : Message =
    { Type = "StringThing.FSharp.UntraceableFormattableString"
      Message =
          "This argument's provenance can't be verified. The analyzer can only check `$\"...\"` it can see. "
          + "Use an inline `$\"...\"`, a local binding whose right-hand side traces to one, or a value typed as SqlStatement / SqlFragment."
      Code = "ST-FS-003"
      Severity = Severity.Error
      Range = range
      Fixes = [] }

let private longIdentNames (longIdent: SynLongIdent) : string list =
    let (SynLongIdent(idents, _, _)) = longIdent
    idents |> List.map (fun id -> id.idText)

/// Returns the function name chain at the bottom of a possibly-curried App.
/// `connection.QueryStringSingle row stmt` → `["connection"; "QueryStringSingle"]`
/// `Sqlite.statement $""` → `["Sqlite"; "statement"]`
let rec private funcNameChain (expr: SynExpr) : string list option =
    match expr with
    | SynExpr.App(_, _, funcExpr, _, _) -> funcNameChain funcExpr
    | SynExpr.TypeApp(funcExpr, _, _, _, _, _, _) -> funcNameChain funcExpr
    | SynExpr.DotGet(_, _, longIdent, _) -> Some (longIdentNames longIdent)
    | SynExpr.LongIdent(_, longIdent, _, _) -> Some (longIdentNames longIdent)
    | SynExpr.Ident id -> Some [ id.idText ]
    | _ -> None

let private lastName (chain: string list) : string option = List.tryLast chain

let private isTargetConnectionMethodName (name: string) : bool =
    targetConnectionMethodNames.Contains name

let private isFactoryMethodChain (chain: string list) : bool =
    match chain with
    | [ "Sqlite"; method ] when factoryMethodNames.Contains method -> true
    | [ method ] when factoryMethodNames.Contains method ->
        // bare `statement` / `fragment` (after `open StringThing.FSharp.Sqlite`)
        true
    | _ -> false

/// All arguments of a (possibly curried) App, in order.
let private appArgs (expr: SynExpr) : SynExpr list =
    let rec loop e acc =
        match e with
        | SynExpr.App(_, _, f, arg, _) -> loop f (arg :: acc)
        | _ -> acc
    loop expr []

/// Walk down an App chain to find the innermost function reference (Ident, LongIdent, DotGet)
/// and return its source range. Used to position symbol lookups precisely on the function name.
let rec private funcReferenceRange (expr: SynExpr) : Range =
    match expr with
    | SynExpr.App(_, _, funcExpr, _, _) -> funcReferenceRange funcExpr
    | SynExpr.TypeApp(funcExpr, _, _, _, _, _, _) -> funcReferenceRange funcExpr
    | _ -> expr.Range

let private patternName (pat: SynPat) : string option =
    let rec loop p =
        match p with
        | SynPat.Named(SynIdent(id, _), _, _, _) -> Some id.idText
        | SynPat.Typed(inner, _, _) -> loop inner
        | SynPat.LongIdent(SynLongIdent([ id ], _, _), _, _, _, _, _) -> Some id.idText
        | SynPat.Paren(inner, _) -> loop inner
        | _ -> None
    loop pat

/// Collect every `let name = expr` binding in the file by name → RHS expression.
/// For shadowing we keep all and require any of them to be blessed at use site.
let private collectBindings (parsedInput: ParsedInput) : Map<string, SynExpr list> =
    let bindings = System.Collections.Generic.List<string * SynExpr>()
    let seen = System.Collections.Generic.HashSet<Range>()
    let addBinding (binding: SynBinding) =
        let (SynBinding(_, _, _, _, _, _, _, pat, _, expr, range, _, _)) = binding
        if seen.Add range then
            match patternName pat with
            | Some name -> bindings.Add(name, expr)
            | None -> ()

    let collector =
        { new SyntaxCollectorBase() with
            override _.WalkBinding(_path, binding) =
                addBinding binding
            override _.WalkExpr(_path, expr) =
                match expr with
                | SynExpr.LetOrUse(_, _, innerBindings, _, _, _) ->
                    for b in innerBindings do addBinding b
                | _ -> () }
    walkAst collector parsedInput
    bindings
    |> Seq.toList
    |> List.groupBy fst
    |> List.map (fun (k, vs) -> k, vs |> List.map snd)
    |> Map.ofList

/// Walk a function type, returning the type after applying all arguments.
let rec private finalReturnType (t: FSharpType) : FSharpType =
    let t = if t.IsAbbreviation then t.AbbreviatedType else t
    if t.IsFunctionType && t.GenericArguments.Count >= 2 then
        finalReturnType t.GenericArguments.[t.GenericArguments.Count - 1]
    else
        t

let private isBlessedWrapperType (t: FSharpType) : bool =
    let t = stripAbbreviation (finalReturnType t)
    if not t.HasTypeDefinition then false
    else
        match tryEntityFullName t.TypeDefinition with
        | Some name -> blessedWrapperFullNames.Contains name
        | None -> false

let private lookupSymbolType
    (checkResults: FSharpCheckFileResults)
    (sourceText: ISourceText)
    (range: Range)
    (names: string list)
    : FSharpType option
    =
    let line = range.End.Line
    let endCol = range.End.Column
    let lineText =
        try sourceText.GetLineString(line - 1)
        with _ -> ""
    let symbolUse =
        try checkResults.GetSymbolUseAtLocation(line, endCol, lineText, names)
        with _ -> None
    match symbolUse with
    | Some su ->
        match su.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            try
                if mfv.IsFunction then
                    Some (finalReturnType mfv.FullType)
                else
                    Some mfv.FullType
            with _ -> None
        | _ -> None
    | None -> None

/// Recursive structural check on the untyped AST.
/// `bindings` is the local-binding map; `visitedNames` tracks identifier names already
/// traced (to prevent cycles like `let x = x` from looping). `lookupType` resolves
/// non-local references.
let rec private isBlessedExpression
    (bindings: Map<string, SynExpr list>)
    (lookupType: Range -> string list -> FSharpType option)
    (visitedNames: System.Collections.Generic.HashSet<string>)
    (expr: SynExpr)
    : bool
    =
    match expr with
    | SynExpr.InterpolatedString _ -> true

    | SynExpr.Paren(inner, _, _, _)
    | SynExpr.Typed(inner, _, _) ->
        isBlessedExpression bindings lookupType visitedNames inner

    | SynExpr.IfThenElse(_, thenE, Some elseE, _, _, _, _) ->
        isBlessedExpression bindings lookupType visitedNames thenE
        && isBlessedExpression bindings lookupType visitedNames elseE

    | SynExpr.Match(_, _, clauses, _, _) ->
        clauses
        |> List.forall (fun (SynMatchClause(_, _, body, _, _, _)) ->
            isBlessedExpression bindings lookupType visitedNames body)

    | SynExpr.Sequential(_, _, _, finalExpr, _, _) ->
        isBlessedExpression bindings lookupType visitedNames finalExpr

    | SynExpr.LetOrUse(_, _, _, body, _, _) ->
        isBlessedExpression bindings lookupType visitedNames body

    | SynExpr.App(_, _, _, argExpr, _) as appExpr ->
        match funcNameChain appExpr with
        | Some chain when isFactoryMethodChain chain ->
            isBlessedExpression bindings lookupType visitedNames argExpr
        | Some chain ->
            let funcRange = funcReferenceRange appExpr
            match lookupType funcRange chain with
            | Some t -> isBlessedWrapperType t
            | None -> false
        | None -> false

    | SynExpr.Ident id when visitedNames.Contains id.idText -> false

    | SynExpr.Ident id ->
        match Map.tryFind id.idText bindings with
        | Some rhsList ->
            visitedNames.Add id.idText |> ignore
            rhsList
            |> List.exists (fun rhs ->
                isBlessedExpression bindings lookupType visitedNames rhs)
        | None ->
            match lookupType id.idRange [ id.idText ] with
            | Some t -> isBlessedWrapperType t
            | None -> false

    | SynExpr.LongIdent(_, longIdent, _, _) ->
        let names = longIdentNames longIdent
        match names with
        | [ single ] when Map.containsKey single bindings && not (visitedNames.Contains single) ->
            visitedNames.Add single |> ignore
            bindings.[single]
            |> List.exists (fun rhs ->
                isBlessedExpression bindings lookupType visitedNames rhs)
        | _ ->
            match lookupType longIdent.Range names with
            | Some t -> isBlessedWrapperType t
            | None -> false

    | SynExpr.DotGet(_, _, _, range) ->
        match funcNameChain expr with
        | Some chain ->
            match lookupType range chain with
            | Some t -> isBlessedWrapperType t
            | None -> false
        | None -> false

    | _ -> false

/// Returns the outermost App of a curried call chain whose method name is `name`,
/// or None if `expr` isn't a target call to that method.
let private isCallToTarget (expr: SynExpr) : bool =
    match expr with
    | SynExpr.App _ ->
        match funcNameChain expr |> Option.bind lastName with
        | Some name -> isTargetConnectionMethodName name
        | None -> false
    | _ -> false

/// Run the ST-FS-003 check on the untyped AST.
let private runProvenanceCheck
    (parsedInput: ParsedInput)
    (checkResults: FSharpCheckFileResults)
    (sourceText: ISourceText)
    (messages: ResizeArray<Message>)
    : unit
    =
    let bindings = collectBindings parsedInput
    let lookupType = lookupSymbolType checkResults sourceText

    let collector =
        { new SyntaxCollectorBase() with
            override _.WalkExpr(path, expr) =
                if isCallToTarget expr then
                    let isOutermost =
                        match path with
                        | SyntaxNode.SynExpr parent :: _ ->
                            match parent with
                            | SynExpr.App(_, _, parentFunc, _, _) ->
                                not (System.Object.ReferenceEquals(parentFunc, expr))
                            | _ -> true
                        | _ -> true
                    if isOutermost then
                        let args = appArgs expr
                        match List.tryLast args with
                        | Some lastArg ->
                            let visited = System.Collections.Generic.HashSet<string>()
                            if not (isBlessedExpression bindings lookupType visited lastArg) then
                                messages.Add(rejectedProvenanceMessage lastArg.Range)
                        | None -> () }

    walkAst collector parsedInput

/// Reports unsupported parameter shapes in StringThing.FSharp.Sqlite interpolated SQL.
///
/// Every inline `$""` expression in a file (any FormattableStringFactory.Create call in the
/// typed tree) has its holes checked. A hole's value type must be:
///
/// - a supported scalar (bool, int, int64, float, string, byte[], Guid, DateTime, options thereof)
/// - `SqlFragment` or `SqlStatement` (built via `Sqlite.fragment` / `Sqlite.statement`)
/// - `obj` (the return type of `Sqlite.unsafe` / `Sqlite.inList` / `Sqlite.insertRows`)
/// - another inline `$""` (analyzer recurses)
///
/// Diagnostic codes:
/// * `ST-FS-001` — unsupported value type in a `$""` hole.
/// * `ST-FS-002` — a `FormattableString`-typed hole that isn't itself an inline `$""` or a
///   `SqlFragment` value. Construct one via `Sqlite.fragment $"..."` instead.
///
/// Known limitation: a raw `FormattableString` value passed as the *outer* argument to a
/// SqliteConnection method (e.g. `let s : FormattableString = $"..."; connection.X s`) isn't
/// flagged at the call site. F# 9 inserts the SqlStatement → FormattableString implicit
/// conversion at binding time, which collapses the type information we'd need to enforce
/// the rule. The runtime dispatch in StringThing.FSharp.Sqlite still rejects unsupported
/// parameters, and any `$""` literal anywhere in the file has its holes checked.
[<CliAnalyzer "StringThingFSharpSqlite.SupportedParameterType">]
let supportedParameterTypeAnalyzer: Analyzer<CliContext> =
    fun ctx ->
        async {
            let messages = ResizeArray<Message>()

            match ctx.TypedTree with
            | None -> ()
            | Some typedTree ->
                let collector =
                    { new TypedTreeCollectorBase() with
                        member _.WalkCall _thisExpr mfv _typeArgs _instArgs args _range =
                            if isFormattableStringFactoryCreate mfv then
                                analyseFormattableStringCreate args messages }

                walkTast collector typedTree

            runProvenanceCheck ctx.ParseFileResults.ParseTree ctx.CheckFileResults ctx.SourceText messages

            return List.ofSeq messages
        }
