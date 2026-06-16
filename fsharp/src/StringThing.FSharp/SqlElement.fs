namespace StringThing.FSharp

type SqlElement<'TParameter> =
    | Literal of string
    | Parameter of 'TParameter

/// Internal tree of fragment nodes. Combine produces Pair in O(1) (no element copies);
/// the tree is flattened in-order at command-build time.
[<NoComparison; NoEquality>]
type internal FragmentNode<'TParameter> =
    | NodeEmpty
    | NodeOne of SqlElement<'TParameter>
    | NodePair of FragmentNode<'TParameter> * FragmentNode<'TParameter>
