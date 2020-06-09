namespace Fasaani

open System.Threading
open Microsoft.Azure.Search.Models

type Lat = Lat of decimal
type Lon = Lon of decimal
type Coordinate = Lat * Lon
type Polygon = Coordinate * Coordinate * Coordinate * Coordinate
type NaN = NaN
type Infinite = Infinite override _.ToString() = "INF"
type NegativeInfinite = NegativeInfinite override _.ToString() = "-INF"

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

type Direction =
    | Asc
    | Desc
    member this.LowerCaseValue =
        match this with
        | Asc -> "asc"
        | Desc -> "desc"

type OrderBy =
    | ByField of field:string * Direction
    | ByGeoDistance of field:string * Coordinate * Direction
    | BySearchScore of Direction

type SearchTermMatchMode =
    | All
    | Any
    member this.SearchMode =
        match this with
        | All -> SearchMode.All
        | Any -> SearchMode.Any

type QuerySyntax =
    | Simple
    | Lucene
    member this.QueryType =
        match this with
        | Simple -> QueryType.Simple
        | Lucene -> QueryType.Full

type Filter =
    | Empty
    | Comparison of field:string * ComparisonOperation * value:obj
    | IsIn of field:string * values:string seq
    | IsInDelimited of field:string * values:string seq * delimiter:char
    | GeoDistance of field:string * Coordinate * ComparisonOperation * value:obj
    | GeoIntersect of field:string * Polygon
    | OData of filter:string
    | Binary of Filter * BinaryOperation * Filter
    | Not of Filter
    static member (+) (a,b) = Binary (a, And, b)
    static member (*) (a,b) = Binary (a, Or, b)
    static member (!) a = Not a

type QueryDetails =
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