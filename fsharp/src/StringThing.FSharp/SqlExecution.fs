namespace StringThing.FSharp

module internal SqlExecution =

    /// Walks the fragment tree in left-to-right order, assigning positional names to parameters via
    /// the provider's <paramref name="nameAt"/> formatter.
    let walk
        (root: FragmentNode<'TParameter>)
        (nameAt: int -> string)
        (onLiteral: string -> unit)
        (onParameter: 'TParameter -> string -> unit) =
        let mutable index = 0
        let rec visit node =
            match node with
            | NodeEmpty -> ()
            | NodeOne (Literal text) -> onLiteral text
            | NodeOne (Parameter parameter) ->
                let name = nameAt index
                onParameter parameter name
                index <- index + 1
            | NodePair (left, right) ->
                visit left
                visit right
        visit root
