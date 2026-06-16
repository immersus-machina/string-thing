module StringThing.FSharp.Analyzers.Core.AnalyzerCore

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open FSharp.Compiler.Symbols.FSharpExprPatterns
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text

// ---- Shared constants ----

let private blessedWrapperFullNames =
    Set.ofList [
        // SqlStatement<'TParameter> and SqlFragment<'TParameter> live in StringThing.FSharp
        // so the wrapper full names are shared across all providers.
        "StringThing.FSharp.SqlStatement`1"
        "StringThing.FSharp.SqlFragment`1"
    ]

let private targetConnectionMethodNames =
    Set.ofList [
        "QueryStringSingle"
        "QueryStringSingleOrDefault"
        "QueryString"
        "ExecuteString"
        "ExecuteStringScalar"
    ]

let private factoryMethodNames = Set.ofList [ "statement"; "fragment" ]

let private formattableStringFactoryFullName =
    "System.Runtime.CompilerServices.FormattableStringFactory"

let private formattableStringFullName = "System.FormattableString"

// ---- Type helpers ----

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

let rec private isSupportedHoleType (config: ProviderConfig) (t: FSharpType) : bool =
    let t = stripAbbreviation t
    if isByteArray t then
        true
    elif t.HasTypeDefinition && isOptionDefinition t.TypeDefinition then
        t.GenericArguments.Count = 1
        && isSupportedHoleType config t.GenericArguments.[0]
    elif t.HasTypeDefinition then
        match tryEntityFullName t.TypeDefinition with
        | Some fullName ->
            config.SupportedScalarFullNames.Contains fullName
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

// ---- Diagnostic messages ----

let private unsupportedHoleMessage (config: ProviderConfig) (range: Range) (typeName: string) : Message =
    { Type = "StringThing.FSharp.UnsupportedParameterType"
      Message =
          sprintf
              "Unsupported StringThing parameter type: '%s'. Use %s.unsafe for raw SQL, %s.inList for IN clauses, or %s.fragment to compose another SQL fragment."
              typeName
              config.DisplayName
              config.DisplayName
              config.DisplayName
      Code = "ST-FS-001"
      Severity = Severity.Error
      Range = range
      Fixes = [] }

let private rejectedFormattableStringHoleMessage (config: ProviderConfig) (range: Range) : Message =
    { Type = "StringThing.FSharp.RawFormattableStringHole"
      Message =
          sprintf
              "A FormattableString hole must be either an inline `$\"...\"` or a value of type SqlFragment (built via %s.fragment). Raw FormattableString values cannot be spliced."
              config.DisplayName
      Code = "ST-FS-002"
      Severity = Severity.Error
      Range = range
      Fixes = [] }

let private rejectedProvenanceMessage (config: ProviderConfig) (range: Range) : Message =
    { Type = "StringThing.FSharp.UntraceableFormattableString"
      Message =
          sprintf
              "This argument's provenance can't be verified. The analyzer can only check `$\"...\"` it can see. Use an inline `$\"...\"`, a local binding whose right-hand side traces to one, or a value typed as SqlStatement / SqlFragment (built via %s.statement / %s.fragment)."
              config.DisplayName
              config.DisplayName
      Code = "ST-FS-003"
      Severity = Severity.Error
      Range = range
      Fixes = [] }

// ---- Symbol detection ----

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

// ---- ST-FS-001 / ST-FS-002: hole-type check on the TAST ----

let private checkHole (config: ProviderConfig) (boxed: FSharpExpr) (sink: ResizeArray<Message>) : unit =
    let inner = stripCoerce boxed
    let innerType = inner.Type
    if isSupportedHoleType config innerType then
        ()
    elif isFormattableStringType innerType then
        match inner with
        | Call(_, mfv, _, _, _) when isFormattableStringFactoryCreate mfv -> ()
        | Call(_, mfv, _, _, _) when isOpImplicitFromBlessedWrapper mfv -> ()
        | _ ->
            sink.Add(rejectedFormattableStringHoleMessage config inner.Range)
    else
        sink.Add(unsupportedHoleMessage config inner.Range (displayName innerType))

let private analyseFormattableStringCreate
    (config: ProviderConfig)
    (args: FSharpExpr list)
    (sink: ResizeArray<Message>) : unit
    =
    match args with
    | _ :: arrayExpr :: _ ->
        match arrayExpr with
        | NewArray(_, elements) ->
            for element in elements do
                checkHole config element sink
        | _ -> ()
    | _ -> ()

// ---- ST-FS-003: outer-argument provenance via the untyped AST ----

let private longIdentNames (longIdent: SynLongIdent) : string list =
    let (SynLongIdent(idents, _, _)) = longIdent
    idents |> List.map (fun id -> id.idText)

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

let private isFactoryMethodChain (config: ProviderConfig) (chain: string list) : bool =
    match chain with
    | [ qualifier; method ] when qualifier = config.FactoryQualifier && factoryMethodNames.Contains method -> true
    | [ method ] when factoryMethodNames.Contains method ->
        // bare `statement` / `fragment` after `open StringThing.FSharp.<Provider>`
        true
    | _ -> false

let private appArgs (expr: SynExpr) : SynExpr list =
    let rec loop e acc =
        match e with
        | SynExpr.App(_, _, f, arg, _) -> loop f (arg :: acc)
        | _ -> acc
    loop expr []

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

let rec private isBlessedExpression
    (config: ProviderConfig)
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
        isBlessedExpression config bindings lookupType visitedNames inner

    | SynExpr.IfThenElse(_, thenE, Some elseE, _, _, _, _) ->
        isBlessedExpression config bindings lookupType visitedNames thenE
        && isBlessedExpression config bindings lookupType visitedNames elseE

    | SynExpr.Match(_, _, clauses, _, _) ->
        clauses
        |> List.forall (fun (SynMatchClause(_, _, body, _, _, _)) ->
            isBlessedExpression config bindings lookupType visitedNames body)

    | SynExpr.Sequential(_, _, _, finalExpr, _, _) ->
        isBlessedExpression config bindings lookupType visitedNames finalExpr

    | SynExpr.LetOrUse(_, _, _, body, _, _) ->
        isBlessedExpression config bindings lookupType visitedNames body

    | SynExpr.App(_, _, _, argExpr, _) as appExpr ->
        match funcNameChain appExpr with
        | Some chain when isFactoryMethodChain config chain ->
            isBlessedExpression config bindings lookupType visitedNames argExpr
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
                isBlessedExpression config bindings lookupType visitedNames rhs)
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
                isBlessedExpression config bindings lookupType visitedNames rhs)
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

let private isCallToTarget (expr: SynExpr) : bool =
    match expr with
    | SynExpr.App _ ->
        match funcNameChain expr |> Option.bind lastName with
        | Some name -> isTargetConnectionMethodName name
        | None -> false
    | _ -> false

let private runProvenanceCheck
    (config: ProviderConfig)
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
                            if not (isBlessedExpression config bindings lookupType visited lastArg) then
                                messages.Add(rejectedProvenanceMessage config lastArg.Range)
                        | None -> () }

    walkAst collector parsedInput

// ---- Entry point ----

/// Run the StringThing.FSharp analyzer suite (ST-FS-001/002/003) against a CliContext
/// using the supplied provider configuration. Each provider's analyzer is just a thin
/// wrapper that supplies its config and routes the call here.
let analyze (config: ProviderConfig) (ctx: CliContext) : Async<Message list> =
    async {
        let messages = ResizeArray<Message>()

        match ctx.TypedTree with
        | None -> ()
        | Some typedTree ->
            let collector =
                { new TypedTreeCollectorBase() with
                    member _.WalkCall _thisExpr mfv _typeArgs _instArgs args _range =
                        if isFormattableStringFactoryCreate mfv then
                            analyseFormattableStringCreate config args messages }

            walkTast collector typedTree

        runProvenanceCheck config ctx.ParseFileResults.ParseTree ctx.CheckFileResults ctx.SourceText messages

        return List.ofSeq messages
    }
