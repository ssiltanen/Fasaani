[<AutoOpen>]
module Fasaani.FilterBuilder

let where field comparison value =
    Field (field, comparison, value)

let combineWithAnd : Filter seq -> Filter =
    Seq.fold (+) Filter.Empty

let combineWithOr : Filter seq -> Filter =
    Seq.fold (*) Filter.Empty

let internal evaluate filter =
    let rec eval = function
        | Empty -> None
        | Field (field, comparison, value) ->
            match value with
            | null -> sprintf "%s %s null" field comparison.LowerCaseValue
            | :? string as str -> sprintf "%s %s '%s'" field comparison.LowerCaseValue str
            | value -> sprintf "%s %s %O" field comparison.LowerCaseValue value
            |> Some
        | OData field -> Some field
        | Binary (filter1, operator, filter2) ->
            match eval filter1, eval filter2 with
            | Some f1, Some f2 -> sprintf "(%s %s %s)" f1 operator.LowerCaseValue f2 |> Some
            | Some f, None | None, Some f -> Some f
            | None, None -> None
    eval filter