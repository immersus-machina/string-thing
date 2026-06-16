namespace StringThing.FSharp

open System
open System.Data.Common

[<NoComparison; NoEquality>]
type RowReader<'T> =
    internal {
        Columns: string list
        Materialize: DbDataReader * int[] * int -> 'T
    }

module Row =

    let inline private single name read : RowReader<'T> =
        { Columns = [name]
          Materialize = fun (r, ords, off) -> read r ords.[off] }

    let int (name: string) : RowReader<int> =
        single name (fun r o -> r.GetInt32(o))

    let int64 (name: string) : RowReader<int64> =
        single name (fun r o -> r.GetInt64(o))

    let bool (name: string) : RowReader<bool> =
        single name (fun r o -> r.GetBoolean(o))

    let float (name: string) : RowReader<float> =
        single name (fun r o -> r.GetDouble(o))

    let string (name: string) : RowReader<string> =
        single name (fun r o -> r.GetString(o))

    let stringOption (name: string) : RowReader<string option> =
        single name (fun r o ->
            if r.IsDBNull(o) then None else Some (r.GetString(o)))

    let intOption (name: string) : RowReader<int option> =
        single name (fun r o ->
            if r.IsDBNull(o) then None else Some (r.GetInt32(o)))

    let int64Option (name: string) : RowReader<int64 option> =
        single name (fun r o ->
            if r.IsDBNull(o) then None else Some (r.GetInt64(o)))

    let guid (name: string) : RowReader<Guid> =
        single name (fun r o -> r.GetGuid(o))

    let dateTime (name: string) : RowReader<DateTime> =
        single name (fun r o -> r.GetDateTime(o))

    let bytes (name: string) : RowReader<byte[]> =
        single name (fun r o -> r.GetFieldValue<byte[]>(o))

    /// Read column 0 as <typeparamref name="'T"/>. Use when the query returns a single scalar value.
    let scalar<'T> : RowReader<'T> =
        { Columns = []
          Materialize = fun (r, _, _) -> r.GetFieldValue<'T>(0) }

    /// Read column 0 as <typeparamref name="'T option"/> — <c>None</c> when the column is SQL NULL.
    let scalarOption<'T> : RowReader<'T option> =
        { Columns = []
          Materialize = fun (r, _, _) ->
              if r.IsDBNull(0) then None else Some (r.GetFieldValue<'T>(0)) }

    let internal map (f: 'A -> 'B) (reader: RowReader<'A>) : RowReader<'B> =
        { Columns = reader.Columns
          Materialize = fun args -> f (reader.Materialize args) }

    let internal merge (a: RowReader<'A>) (b: RowReader<'B>) : RowReader<'A * 'B> =
        let leftCount = List.length a.Columns
        { Columns = a.Columns @ b.Columns
          Materialize = fun (r, ords, off) ->
              let av = a.Materialize (r, ords, off)
              let bv = b.Materialize (r, ords, off + leftCount)
              (av, bv) }

    let internal constant (value: 'T) : RowReader<'T> =
        { Columns = []
          Materialize = fun _ -> value }

/// Computation expression builder for declaring row readers via <c>let! / and!</c>.
///
/// <code>
/// let userRow : RowReader&lt;User&gt; =
///     row {
///         let! id    = Row.int64 "id"
///         and! name  = Row.string "name"
///         and! email = Row.stringOption "email"
///         return { Id = id; Name = name; Email = email }
///     }
/// </code>
///
/// <c>let!</c> introduces the first column read; each <c>and!</c> adds another independent read.
/// <c>return</c> assembles the bound values. Forgetting a column results in an "unbound value"
/// error on the <c>return</c> line, pointing at the missing name.
type RowBuilder() =
    member _.Return(value: 'T) : RowReader<'T> =
        Row.constant value

    member _.BindReturn(reader: RowReader<'A>, f: 'A -> 'B) : RowReader<'B> =
        Row.map f reader

    member _.MergeSources(a: RowReader<'A>, b: RowReader<'B>) : RowReader<'A * 'B> =
        Row.merge a b

[<AutoOpen>]
module RowBuilder =
    let row = RowBuilder()
