namespace StringThing.FSharp

/// Base computation expression builder shared between providers.
/// Providers add their own `Run` to capture call-site info and produce a provider-specific Sql.
type SqlBuilderBase<'TParameter>() =
    member _.Yield(fragment: SqlFragment<'TParameter>) : SqlFragment<'TParameter> =
        fragment

    member _.Yield(()) : SqlFragment<'TParameter> =
        SqlFragment NodeEmpty

    member _.Combine(SqlFragment a, SqlFragment b) : SqlFragment<'TParameter> =
        match a, b with
        | NodeEmpty, _ -> SqlFragment b
        | _, NodeEmpty -> SqlFragment a
        | _, _ -> SqlFragment (NodePair (a, b))

    member _.Delay(f: unit -> SqlFragment<'TParameter>) : SqlFragment<'TParameter> =
        f()

    member _.Zero() : SqlFragment<'TParameter> =
        SqlFragment NodeEmpty
