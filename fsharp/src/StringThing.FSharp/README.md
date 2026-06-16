# StringThing.FSharp

Core abstractions for the [StringThing.FSharp](https://github.com/immersus-machina/string-thing/tree/main/fsharp) family. Provider-agnostic. Not intended for direct use — install a provider package such as `StringThing.FSharp.Sqlite`.

## What's in here

- `SqlElement<'TParameter>` — DU of `Literal of string` / `Parameter of 'TParameter`.
- `FragmentNode<'TParameter>` — internal binary tree the providers build their statements into; `Combine` is O(1).
- `SqlExecution.walk` — flattens the tree, invoking provider-supplied callbacks per literal and per parameter, with a provider-specific name formatter.
- `RowReader<'T>` — opaque applicative reader over `DbDataReader`. Built with the `row { let! ... and! ... return ... }` CE.
- `Row` module — column readers (`Row.int64 "id"`, `Row.stringOption "email"`, etc.) and the scalar shortcuts `Row.scalar` / `Row.scalarOption`.
- `OrdinalCache` — internal `ConcurrentDictionary` keyed on `(format string, row type)`, populated lazily per call site.

## Row reader CE

```fsharp
open StringThing.FSharp

type User = { Id: int64; Name: string; Email: string option }

let userRow : RowReader<User> =
    row {
        let! id    = Row.int64 "id"
        and! name  = Row.string "name"
        and! email = Row.stringOption "email"
        return { Id = id; Name = name; Email = email }
    }
```

The CE uses F#'s built-in applicative syntax (`let! / and!` — also seen in `async { }`, `task { }`, `option { }`). Each column reader contributes its name to a list and its read action to a chain. The provider resolves the column names to ordinals once per call site (cached), then applies the materializer chain per row.

Forgetting a column produces an error on the `return` line at the unbound name — local to the block.

## Column readers

`Row.int`, `Row.int64`, `Row.bool`, `Row.float`, `Row.string`, `Row.stringOption`, `Row.intOption`, `Row.int64Option`, `Row.guid`, `Row.dateTime`, `Row.bytes`.

## Scalar readers

`Row.scalar<'T>` and `Row.scalarOption<'T>` read column 0 of the result directly. Use when the query returns a single scalar:

```fsharp
let count : int64 =
    connection.QueryStringSingle Row.scalar $"""
        SELECT COUNT(*) FROM users
        """
```

---

Built by [Immersus Machina](https://www.immersus-machina.com)
