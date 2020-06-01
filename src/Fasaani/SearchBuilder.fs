[<AutoOpen>]
module Fasaani.SearchBuilder

open System
open Microsoft.Azure.Search.Models

type SearchBuilder () =

    member _.Yield _ =
        { Text = None
          Parameters = SearchParameters()
          RequestOptions = None }

    /// Sets searched text value
    [<CustomOperation"searchText">]
    member _.SearchText(state: SearchDetails, text) =
        { state with Text = Some text }

    /// Sets the search filter
    [<CustomOperation"filter">]
    member _.Filter(state: SearchDetails, filter: Filter) =
        state.Parameters.Filter <- evaluate filter |> Option.toObj
        state

    /// Sets the search facets
    [<CustomOperation"facets">]
    member _.Facets(state: SearchDetails, facets: string list) =
        state.Parameters.Facets <- ResizeArray facets
        state

    /// Sets the skipped result count
    [<CustomOperation"skip">]
    member _.Skip(state: SearchDetails, count: int) =
        state.Parameters.Skip <- Nullable count
        state

    /// Sets the max returned results count
    [<CustomOperation"top">]
    member _.Top(state: SearchDetails, count: int) =
        state.Parameters.Top <- Nullable count
        state

    /// Sets query to return the total found results count
    [<CustomOperation"includeTotalResultCount">]
    member _.IncludeTotalResultCount(state: SearchDetails) =
        state.Parameters.IncludeTotalResultCount <- true
        state

    /// Sets query parameters. Used to fall back to raw parameter settings.
    /// Do not use with filter, facets, skip, top, orderBy since these overwrite each other.
    [<CustomOperation"parameters">]
    member _.Parameters(state: SearchDetails, parameters: SearchParameters) =
        { state with Parameters = parameters }

    /// Sets search request options for query.
    [<CustomOperation"requestOptions">]
    member _.RequestOptions(state: SearchDetails, options: SearchRequestOptions) =
        { state with RequestOptions = Some options }

let search = SearchBuilder ()