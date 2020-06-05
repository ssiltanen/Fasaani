[<AutoOpen>]
module Fasaani.QueryBuilder

open System
open Microsoft.Azure.Search.Models

type QueryBuilder () =

    member _.Yield _ =
        { Text = None
          Parameters = SearchParameters()
          RequestOptions = None }

    /// Sets searched text value
    [<CustomOperation"searchText">]
    member _.SearchText(state: QueryDetails, text) =
        { state with Text = Some text }

    /// Sets the query search mode to set whether any or all of the search terms must be matched
    [<CustomOperation"searchMode">]
    member _.SearchMode(state: QueryDetails, mode : Fasaani.SearchTermMatchMode) =
        state.Parameters.SearchMode <- mode.SearchMode
        state

    /// Sets the query syntax to simple or full aka lucene
    [<CustomOperation"querySyntax">]
    member _.QuerySyntax(state: QueryDetails, queryType : Fasaani.QuerySyntax) =
        state.Parameters.QueryType <- queryType.QueryType
        state

    /// Sets the search filter
    [<CustomOperation"filter">]
    member _.Filter(state: QueryDetails, filter: Filter) =
        state.Parameters.Filter <- Filter.evaluate filter |> Option.toObj
        state

    /// Sets the search fields that query is matched against. If none are provided, query matches against all fields
    [<CustomOperation"searchFields">]
    member _.SearchFields(state: QueryDetails, fields : string seq) =
        state.Parameters.SearchFields <- ResizeArray fields
        state

    /// Sets the search facets
    [<CustomOperation"facets">]
    member _.Facets(state: QueryDetails, facets: string list) =
        state.Parameters.Facets <- ResizeArray facets
        state

    /// Sets the skipped result count
    [<CustomOperation"skip">]
    member _.Skip(state: QueryDetails, count: int) =
        state.Parameters.Skip <- Nullable count
        state

    /// Sets the max returned results count
    [<CustomOperation"top">]
    member _.Top(state: QueryDetails, count: int) =
        state.Parameters.Top <- Nullable count
        state

    /// Sets the search order by values
    [<CustomOperation"order">]
    member _.Order(state: QueryDetails, orderBy: OrderBy seq) =
        state.Parameters.OrderBy <- orderBy |> Seq.map OrderBy.evaluate |> ResizeArray
        state

    /// Sets query to return the total found results count
    [<CustomOperation"includeTotalResultCount">]
    member _.IncludeTotalResultCount(state: QueryDetails) =
        state.Parameters.IncludeTotalResultCount <- true
        state

    /// Sets query parameters. Used to fall back to raw parameter settings.
    /// Do not use with filter, facets, skip, top, orderBy since these overwrite each other.
    [<CustomOperation"parameters">]
    member _.Parameters(state: QueryDetails, parameters: SearchParameters) =
        { state with Parameters = parameters }

    /// Sets search request options for query.
    [<CustomOperation"requestOptions">]
    member _.RequestOptions(state: QueryDetails, options: SearchRequestOptions) =
        { state with RequestOptions = Some options }

let query = QueryBuilder ()