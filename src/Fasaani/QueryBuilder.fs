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

    /// Sets a value that specifies whether any or all of the search terms must be matched in order to count the document as a match.
    /// Possible values include: Any, All
    [<CustomOperation"searchMode">]
    member _.SearchMode(state: QueryDetails, mode : Fasaani.SearchTermMatchMode) =
        state.Parameters.SearchMode <- mode.SearchMode
        state

    /// Sets the syntax of the search query.
    /// The default is 'simple'. Use 'Lucene' if your query uses the Lucene query syntax.
    /// Possible values include: Simple, Lucene
    [<CustomOperation"querySyntax">]
    member _.QuerySyntax(state: QueryDetails, queryType : Fasaani.QuerySyntax) =
        state.Parameters.QueryType <- queryType.QueryType
        state

    /// Sets the search filter
    [<CustomOperation"filter">]
    member _.Filter(state: QueryDetails, filter: Filter) =
        state.Parameters.Filter <- Filter.evaluate filter |> Option.toObj
        state

    /// Sets the list of field names to which to scope the full-text search.
    /// When using fielded search (fieldName:searchExpression) in a full Lucene query, the field names of each fielded search expression take precedence over any field names listed in this parameter.
    [<CustomOperation"searchFields">]
    member _.SearchFields(state: QueryDetails, fields : string seq) =
        state.Parameters.SearchFields <- ResizeArray fields
        state

    /// Sets the list of facet expressions to apply to the search query.
    /// Each facet expression contains a field name, optionally followed by a comma-separated list of name:value pairs.
    [<CustomOperation"facets">]
    member _.Facets(state: QueryDetails, facets: string list) =
        state.Parameters.Facets <- ResizeArray facets
        state

    /// Sets the number of search results to skip. This value cannot be greater than 100 000.
    /// If you need to scan documents in sequence, but cannot use skip due to this limitation, consider using orderby on a totally-ordered key and $filter with a range query instead.
    [<CustomOperation"skip">]
    member _.Skip(state: QueryDetails, count: int) =
        state.Parameters.Skip <- Nullable count
        state

    /// Sets the number of search results to retrieve. This can be used in conjunction with skip to implement client-side paging of search results.
    /// If results are truncated due to server-side paging, the response will include a continuation token that can be used to issue another Search request for the next page of results.
    [<CustomOperation"top">]
    member _.Top(state: QueryDetails, count: int) =
        state.Parameters.Top <- Nullable count
        state

    /// Sets the list of expressions by which to sort the results.
    /// Each expression can be either a byField, byGeoDistance or bySearchScore.
    /// Each expression is followed by Asc or Desc to.
    /// Ties will be broken by the match scores of documents.
    /// If no OrderBy is specified, the default sort order is descending by document match score.
    /// There can be at most 32 order by clauses.
    [<CustomOperation"order">]
    member _.Order(state: QueryDetails, orderBy: OrderBy seq) =
        state.Parameters.OrderBy <- orderBy |> Seq.map OrderBy.evaluate |> ResizeArray
        state

    /// Sets a value that specifies whether to fetch the total count of results.
    /// Default is false. Setting this value to true may have a performance impact.
    /// Note that the count returned is an approximation.
    [<CustomOperation"includeTotalResultCount">]
    member _.IncludeTotalResultCount(state: QueryDetails) =
        state.Parameters.IncludeTotalResultCount <- true
        state

    /// Sets the list of field names to use for hit highlights. Only searchable fields can be used for hit highlighting
    [<CustomOperation"highlightFields">]
    member _.HighlightFields(state: QueryDetails, fields: string seq) =
        state.Parameters.HighlightFields <- ResizeArray fields
        state

    /// Sets pre and post string tags that are prepended and appended to hit highlights.
    /// Default pre tag is &amp;lt;em&amp;gt; and default post tag is &amp;lt;/em&amp;gt;.
    /// Both tags are required
    [<CustomOperation"highlightTags">]
    member _.HighlightTags(state: QueryDetails, (Pre preTag), (Post postTag)) =
        state.Parameters.HighlightPreTag <- preTag
        state.Parameters.HighlightPostTag <- postTag
        state

    /// Sets a number between 0 and 100 indicating the percentage of the index that must be covered by a search query in order for the query to be reported as a success.
    /// This parameter can be useful for ensuring search availability even for services with only one replica.
    /// The default is 100.
    [<CustomOperation"minCoverage">]
    member _.MinCoverage(state: QueryDetails, minCoverage) =
        state.Parameters.MinimumCoverage <- Option.toNullable minCoverage
        state

    /// Sets the name of a scoring profile to evaluate match scores for matching documents in order to sort the results.
    [<CustomOperation"scoringProfile">]
    member _.ScoringProfile(state: QueryDetails, profile) =
        state.Parameters.ScoringProfile <- profile
        state

    /// Sets the list of parameter values to be used in scoring functions (for example, referencePointParameter) using the format (name, [ values ]).
    [<CustomOperation"scoringParameters">]
    member _.ScoringParameters(state: QueryDetails, parameters: (string * ScoringParameterValue) seq) =
        state.Parameters.ScoringParameters <-
            parameters
            |> Seq.map (fun (name, value) ->
                match value with
                | GeoPoint (Lat lat, Lon lon) -> ScoringParameter(name, Microsoft.Spatial.GeographyPoint.Create(float lat, float lon))
                | Values values -> ScoringParameter(name, values))
            |> ResizeArray
        state

    /// Sets the tracking ID sent with the request to help with debugging
    [<CustomOperation"clientRequestId">]
    member _.ClientRequestId(state: QueryDetails, requestId) =
        { state with RequestOptions = requestId |> Option.toNullable |> SearchRequestOptions |> Some }

    /// Sets query parameters. Used to fall back to raw parameter settings.
    /// Do not use with filter, facets, skip, top, orderBy since these overwrite each other.
    [<Obsolete("This property is obsolete. Use other query builder methods instead.", false)>]
    [<CustomOperation"parameters">]
    member _.Parameters(state: QueryDetails, parameters: SearchParameters) =
        { state with Parameters = parameters }

    /// Sets search request options for query.
    [<Obsolete("This property is obsolete. clientRequestId instead.", false)>]
    [<CustomOperation"requestOptions">]
    member _.RequestOptions(state: QueryDetails, options: SearchRequestOptions) =
        { state with RequestOptions = Some options }

let query = QueryBuilder ()