namespace StringThing.FSharp

open System

/// A top-level SQL statement wrapping a `FormattableString`. The `'TParameter`
/// generic argument ties the statement to a specific provider's parameter type.
/// Has an `op_Implicit` conversion to `FormattableString` so it flows naturally
/// into provider connection methods that accept `FormattableString`.
[<Sealed; NoComparison; NoEquality>]
type SqlStatement<'TParameter> (formattable: FormattableString) =
    member _.Formattable = formattable
    static member op_Implicit (statement: SqlStatement<'TParameter>) : FormattableString = statement.Formattable

/// A composable SQL fragment wrapping a `FormattableString`. Embed inside another
/// `$""` to splice it into a larger statement; parameters renumber automatically.
[<Sealed; NoComparison; NoEquality>]
type SqlFragment<'TParameter> (formattable: FormattableString) =
    member _.Formattable = formattable
    static member op_Implicit (fragment: SqlFragment<'TParameter>) : FormattableString = fragment.Formattable
