namespace StringThing.FSharp.SqlClient

#nowarn "3391"

open System
open System.Data.Common
open System.Globalization
open System.Text.RegularExpressions
open Microsoft.Data.SqlClient
open StringThing.FSharp

// ---- Provider-specific aliases for the generic wrapper types from Core ----

type SqlServerStatement = SqlStatement<SqlParameter>
type SqlServerFragment = SqlFragment<SqlParameter>

// ---- Marker types for special interpolation arguments ----

type internal UnsafeMarker = { Raw: string }

type internal InListMarker = { Items: obj[] }

type internal InsertRowsMarker = { Rows: SqlServerFragment[] }

// ---- Value -> SqlElement dispatch ----

module internal ParameterDispatch =

    let private optionGenericDef = typedefof<_ option>

    let rec toElements (arg: obj | null) : seq<SqlElement<SqlParameter>> =
        match arg with
        | null ->
            seq { yield Parameter (SqlParameter(Value = DBNull.Value)) }
        | :? UnsafeMarker as marker ->
            seq { yield Literal marker.Raw }
        | :? InListMarker as marker ->
            seq {
                yield Literal "("
                for i = 0 to marker.Items.Length - 1 do
                    if i > 0 then yield Literal ", "
                    yield! toElements marker.Items.[i]
                yield Literal ")"
            }
        | :? InsertRowsMarker as marker ->
            seq {
                for i = 0 to marker.Rows.Length - 1 do
                    if i > 0 then yield Literal ", "
                    yield! ofFormattable marker.Rows.[i].Formattable
            }
        | :? SqlServerFragment as fragment ->
            ofFormattable fragment.Formattable
        | :? SqlServerStatement as statement ->
            ofFormattable statement.Formattable
        | :? FormattableString as nested ->
            ofFormattable nested
        | :? bool as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? int as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? int64 as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? float as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? decimal as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? string as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? Guid as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? DateTime as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? DateTimeOffset as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? TimeSpan as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | :? (byte[]) as v -> seq { yield Parameter (SqlParameter(Value = v)) }
        | other ->
            let argType = other.GetType()
            if argType.IsGenericType && argType.GetGenericTypeDefinition() = optionGenericDef then
                match argType.GetProperty("Value") with
                | null -> invalidOp (sprintf "F# Option type %s has no Value property — internal error." argType.FullName)
                | valueProp -> toElements (valueProp.GetValue(other))
            else
                invalidOp (sprintf
                    "Unsupported parameter type: %s. Use SqlServer.unsafe for raw SQL, SqlServer.inList for IN clauses, or install StringThing.FSharp.SqlClient.Analyzers to catch this at compile time."
                    argType.FullName)

    and private ofFormattable (fs: FormattableString) : seq<SqlElement<SqlParameter>> =
        let placeholderPattern = Regex(@"\{(\d+)(?::[^}]*)?\}", RegexOptions.Compiled)
        let unescape (s: string) = s.Replace("{{", "{").Replace("}}", "}")
        let args = fs.GetArguments()
        let format = fs.Format
        seq {
            let mutable lastEnd = 0
            for m in placeholderPattern.Matches(format) do
                if m.Index > lastEnd then
                    yield Literal (unescape (format.Substring(lastEnd, m.Index - lastEnd)))
                let argIndex = Int32.Parse(m.Groups.[1].Value, CultureInfo.InvariantCulture)
                yield! toElements args.[argIndex]
                lastEnd <- m.Index + m.Length
            if lastEnd < format.Length then
                yield Literal (unescape (format.Substring(lastEnd)))
        }

    let parse (fs: FormattableString) : FragmentNode<SqlParameter> =
        let mutable root : FragmentNode<SqlParameter> = NodeEmpty
        for el in ofFormattable fs do
            root <-
                match root with
                | NodeEmpty -> NodeOne el
                | _ -> NodePair (root, NodeOne el)
        root

// ---- Public helpers ----

[<AbstractClass; Sealed>]
type SqlServer =
    /// Wraps an interpolated string as a top-level SQL statement.
    static member statement (formattable: FormattableString) : SqlServerStatement =
        SqlStatement(formattable)

    /// Wraps an interpolated string as a composable SQL fragment.
    static member fragment (formattable: FormattableString) : SqlServerFragment =
        SqlFragment(formattable)

    /// Splice raw, unparameterized SQL text. The caller takes responsibility for safety.
    static member unsafe (raw: string) : obj =
        { Raw = raw } :> obj

    /// Expand a sequence of values into a parenthesized, comma-separated <c>IN</c> list.
    /// Throws if the sequence is empty.
    static member inList (values: 'T seq) : obj =
        let items = values |> Seq.cast<obj> |> Seq.toArray
        if items.Length = 0 then invalidArg "values" "At least one value is required."
        { Items = items } :> obj

    /// Compose multiple row fragments into a comma-separated VALUES list for multi-row INSERTs.
    /// Throws if the sequence is empty.
    static member insertRows (toRow: 'T -> SqlServerFragment) (rows: 'T seq) : obj =
        let fragments = rows |> Seq.map toRow |> Seq.toArray
        if fragments.Length = 0 then invalidArg "rows" "At least one row is required."
        { Rows = fragments } :> obj

// ---- Connection extensions ----

[<AutoOpen>]
module SqlServerConnectionExtensions =

    let internal buildCommand (connection: SqlConnection) (statement: FormattableString) : SqlCommand =
        let command = connection.CreateCommand()
        let sb = System.Text.StringBuilder()
        let root = ParameterDispatch.parse statement
        SqlExecution.walk
            root
            (sprintf "@p%d")
            (fun text -> sb.Append(text) |> ignore)
            (fun parameter name ->
                parameter.ParameterName <- name
                command.Parameters.Add(parameter) |> ignore
                sb.Append(name) |> ignore)
        command.CommandText <- sb.ToString()
        command

    let internal resolveOrdinals (reader: DbDataReader) (cacheKey: string) (rowType: Type) (columns: string list) : int[] =
        if List.isEmpty columns then
            Array.empty
        else
            let key = (cacheKey, 0)
            match OrdinalCache.tryGet key rowType with
            | Some cached -> cached
            | None ->
                let ordinals = columns |> List.map reader.GetOrdinal |> List.toArray
                OrdinalCache.set key rowType ordinals
                ordinals

    type SqlConnection with

        member connection.QueryStringSingle (row: RowReader<'T>) (statement: FormattableString) : 'T =
            use command = buildCommand connection statement
            use reader = command.ExecuteReader()
            let ords = resolveOrdinals reader statement.Format typeof<'T> row.Columns
            if not (reader.Read()) then
                invalidOp "Sequence contains no elements."
            let value = row.Materialize (reader, ords, 0)
            if reader.Read() then
                invalidOp "Sequence contains more than one element."
            value

        member connection.QueryStringSingleOrDefault (row: RowReader<'T>) (statement: FormattableString) : 'T option =
            use command = buildCommand connection statement
            use reader = command.ExecuteReader()
            let ords = resolveOrdinals reader statement.Format typeof<'T> row.Columns
            if not (reader.Read()) then
                None
            else
                let value = row.Materialize (reader, ords, 0)
                if reader.Read() then
                    invalidOp "Sequence contains more than one element."
                Some value

        member connection.QueryString (row: RowReader<'T>) (statement: FormattableString) : seq<'T> =
            seq {
                use command = buildCommand connection statement
                use reader = command.ExecuteReader()
                let ords = resolveOrdinals reader statement.Format typeof<'T> row.Columns
                while reader.Read() do
                    yield row.Materialize (reader, ords, 0)
            }

        member connection.ExecuteString (statement: FormattableString) : int =
            use command = buildCommand connection statement
            command.ExecuteNonQuery()

        member connection.ExecuteStringScalar (statement: FormattableString) : obj option =
            use command = buildCommand connection statement
            command.ExecuteScalar()
            |> Option.ofObj
            |> Option.filter (fun r -> not (r :? System.DBNull))
