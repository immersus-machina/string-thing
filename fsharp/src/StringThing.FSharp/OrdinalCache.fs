namespace StringThing.FSharp

open System
open System.Collections.Concurrent

module internal OrdinalCache =

    let private cache = ConcurrentDictionary<string * int * Type, int[]>()

    let tryGet (filePath: string, lineNumber: int) (rowType: Type) : int[] option =
        match cache.TryGetValue((filePath, lineNumber, rowType)) with
        | true, ords -> Some ords
        | false, _ -> None

    let set (filePath: string, lineNumber: int) (rowType: Type) (ordinals: int[]) : unit =
        cache.[(filePath, lineNumber, rowType)] <- ordinals
