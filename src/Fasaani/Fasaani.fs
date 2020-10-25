namespace Fasaani

open System.Threading
open Microsoft.Azure.Search.Models

type Lat = Lat of decimal
type Lon = Lon of decimal
type Coordinate = Lat * Lon
// Azure Search sets some rules for the polygon coordinates:
// - At least 3 unique coordinates
// - Points in a polygon must be in counterclockwise order
// - The polygon needs to be closed, meaning the first and last point sets must be the same.
// TODO: Create automatisation for the above
type Polygon = Coordinate seq

type ScoringParameterValue =
    | GeoPoint of Coordinate
    | Values of string seq

type BinaryOperation =
    | And
    | Or
    member this.LowerCaseValue =
        this.ToString().ToLowerInvariant()

type ComparisonOperation =
    | Eq
    | Ne
    | Gt
    | Lt
    | Ge
    | Le
    member this.LowerCaseValue =
        this.ToString().ToLowerInvariant()

type Direction =
    | Asc
    | Desc
    member this.LowerCaseValue =
        this.ToString().ToLowerInvariant()

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

type Pre = Pre of string
type Post = Post of string

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
    static member (!!) a = Not a

type QueryDetails =
    { Text: string option
      Parameters: SearchParameters
      RequestOptions: SearchRequestOptions option }
    static member ToString (q: QueryDetails) =
        let flatten =
            Option.ofObj
            >> Option.map (String.concat ", " >> sprintf "[ %s ]")
            >> Option.toObj

        let p = q.Parameters
        let newLine = System.Environment.NewLine
        let scoringParameters =
            p.ScoringParameters
            |> Option.ofObj
            |> Option.map (Seq.map (fun sp -> printf "%s: %O" sp.Name (sp.Values |> flatten)))
            |> Option.toObj

        // Parameters select values are not listed on default logger since they are added behind the scenes
        // But using a custom logger it is possible to log them as they are added to query parameters before
        // queryDefails are given to logger
        let details =
            [ "Filter",             box p.Filter
              "Search Text",        box (Option.toObj q.Text)
              "Query Type",         box (q.Text |> Option.map (fun _ -> string p.QueryType) |> Option.toObj)
              "Search Mode",        box (q.Text |> Option.map (fun _ -> string p.SearchMode) |> Option.toObj)
              "Search Fields",      box (flatten p.SearchFields)
              "Highlight Fields",   box (flatten p.HighlightFields)
              "Highlight Post Tag", box p.HighlightPostTag
              "Highlight Pre Tag",  box p.HighlightPreTag
              "Minimum Coverage",   box p.MinimumCoverage
              "Scoring Profile",    box p.ScoringProfile
              "Scoring Parameters", box scoringParameters
              "Facets",             box (flatten p.Facets)
              "Skip",               box p.Skip
              "Top",                box p.Top
              "Order By",           box (flatten p.OrderBy)
              "clientRequestId",    box (q.RequestOptions |> Option.bind (fun opt -> opt.ClientRequestId |> Option.ofNullable)) ]
           |> List.where (snd >> unbox >> isNull >> not)
           |> function
           | [] -> ""
           | details ->
                details
                |> List.map (fun (name, value) -> sprintf "%s: %O" name value)
                |> String.concat (newLine + "    ")
                |> sprintf "%s    %s" newLine

        sprintf "Query {%s%s    Count Total Results: %b%s}"
            details newLine p.IncludeTotalResultCount newLine


type SearchResult<'T> =
    { Results: 'T seq
      Facets: Map<string, string seq>
      Count: int64 option
      Raw: DocumentSearchResult<'T> }

type SearchConfig =
    { Log: (QueryDetails -> unit) option
      CancellationToken: CancellationToken option }