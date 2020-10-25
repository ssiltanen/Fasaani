[<AutoOpen>]
module Fasaani.Search

open System
open System.Collections.Generic
open System.Threading
open FSharp.Control.Tasks
open Microsoft.Azure.Search
open Microsoft.Azure.Search.Models

let internal recordFields (t: Type) =
    Reflection.FSharpType.GetRecordFields t
    |> Array.map (fun x -> x.Name)

let internal parseFacets (facets: IDictionary<string, IList<FacetResult>>) =
    facets
    |> Option.ofObj
    |> Option.map (
        Seq.map (fun pair -> pair.Key, pair.Value |> Seq.map (fun facet -> string facet.Value))
        >> Map)
    |> Option.defaultValue Map.empty

let internal searchIndex<'T> (client: ISearchIndexClient) (config: SearchConfig option) (query: QueryDetails) =
    // Return only fields used in the return record type
    query.Parameters.Select <- recordFields (typeof<'T>)

    config
    |> Option.bind (fun c -> c.Log)
    |> Option.iter (fun log -> log query)

    task {
        let! searchResult =
            client.Documents.SearchAsync<'T>(
                query.Text |> Option.toObj,
                query.Parameters,
                query.RequestOptions |> Option.toObj,
                config |> Option.bind (fun c -> c.CancellationToken) |> Option.defaultValue CancellationToken.None)

        return
            { Results = searchResult.Results |> Seq.map (fun r -> r.Document)
              Facets = parseFacets searchResult.Facets
              Count = Option.ofNullable searchResult.Count
              Raw = searchResult }
    }

let internal continueSearchIndex<'T> (client: ISearchIndexClient) (config: SearchConfig option) (requestId : Guid option) (continuationToken : SearchContinuationToken) =
    let query = query { clientRequestId requestId }
    // Return only fields used in the return record type
    query.Parameters.Select <- recordFields (typeof<'T>)

    config
    |> Option.bind (fun c -> c.Log)
    |> Option.iter (fun log -> log query)

    task {
        let! searchResult =
            client.Documents.ContinueSearchAsync<'T>(
                continuationToken,
                query.RequestOptions |> Option.toObj,
                config |> Option.bind (fun c -> c.CancellationToken) |> Option.defaultValue CancellationToken.None)

        return
            { Results = searchResult.Results |> Seq.map (fun r -> r.Document)
              Facets = parseFacets searchResult.Facets
              Count = Option.ofNullable searchResult.Count
              Raw = searchResult }
    }


let searchWithConfigAsync<'T> (client: ISearchIndexClient) (config: SearchConfig) (details: QueryDetails) =
    searchIndex<'T> client (Some config) details

let searchWithConfig<'T> client config details =
    searchWithConfigAsync<'T> client config details
    |> Async.AwaitTask |> Async.RunSynchronously

let searchAsync<'T> (client: ISearchIndexClient) (details: QueryDetails) =
    searchIndex<'T> client None details

let search<'T> client details =
    searchAsync<'T> client details
    |> Async.AwaitTask |> Async.RunSynchronously


/// If Azure Cognitive Search can't include all results in a single response, the response returned will include a continuation token that can be passed to ContinueSearch to retrieve more results.
/// See DocumentSearchResult.ContinuationToken for more information.
/// Note that this method is not meant to help you implement paging of search results.
/// You can implement paging using the Top and Skip parameters to the Search method.
let continueSearchWithConfigAsync<'T> (client: ISearchIndexClient) (config: SearchConfig) (requestId : Guid option) (continuationToken : SearchContinuationToken) =
    continueSearchIndex<'T> client (Some config) requestId continuationToken

/// If Azure Cognitive Search can't include all results in a single response, the response returned will include a continuation token that can be passed to ContinueSearch to retrieve more results.
/// See DocumentSearchResult.ContinuationToken for more information.
/// Note that this method is not meant to help you implement paging of search results.
/// You can implement paging using the Top and Skip parameters to the Search method.
let continueSearchWithConfig<'T> client config requestId continuationToken =
    continueSearchWithConfigAsync<'T> client config requestId continuationToken
    |> Async.AwaitTask |> Async.RunSynchronously

/// If Azure Cognitive Search can't include all results in a single response, the response returned will include a continuation token that can be passed to ContinueSearch to retrieve more results.
/// See DocumentSearchResult.ContinuationToken for more information.
/// Note that this method is not meant to help you implement paging of search results.
/// You can implement paging using the Top and Skip parameters to the Search method.
let continueSearchAsync<'T> (client: ISearchIndexClient) (requestId : Guid option) (continuationToken : SearchContinuationToken) =
    continueSearchIndex<'T> client None requestId continuationToken

/// If Azure Cognitive Search can't include all results in a single response, the response returned will include a continuation token that can be passed to ContinueSearch to retrieve more results.
/// See DocumentSearchResult.ContinuationToken for more information.
/// Note that this method is not meant to help you implement paging of search results.
/// You can implement paging using the Top and Skip parameters to the Search method.
let continueSearch<'T> client requestId continuationToken =
    continueSearchAsync<'T> client requestId continuationToken
    |> Async.AwaitTask |> Async.RunSynchronously