namespace Fasaani

open System.Threading
open Microsoft.Azure.Search.Models

type BinaryOperation =
    | And
    | Or
    member this.LowerCaseValue =
        match this with
        | And -> "and"
        | Or -> "or"

type ComparisonOperation =
    | Eq
    | Ne
    | Gt
    | Lt
    | Ge
    | Le
    member this.LowerCaseValue =
        match this with
        | Eq -> "eq"
        | Ne -> "ne"
        | Gt -> "gt"
        | Lt -> "lt"
        | Ge -> "ge"
        | Le -> "le"

type Filter =
    | Empty
    | Field of field:string * ComparisonOperation * value:obj
    | OData of filter:string
    | Binary of Filter * BinaryOperation * Filter
    static member (+) (a,b) = Binary (a, And, b)
    static member (*) (a,b) = Binary (a, Or, b)

type SearchDetails =
    { Text: string option
      Parameters: SearchParameters
      RequestOptions: SearchRequestOptions option }

type SearchResult<'T> =
    { Results: 'T seq
      Facets: Map<string, string seq>
      Count: int64 option
      Raw: DocumentSearchResult<'T> }

type SearchConfig =
    { Log: (string option -> SearchParameters -> SearchRequestOptions option -> unit) option
      CancellationToken: CancellationToken option }