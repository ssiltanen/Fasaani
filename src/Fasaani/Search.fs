[<AutoOpen>]
module Fasaani.Search

open System
open System.Threading
open FSharp.Control.Tasks
open Microsoft.Azure.Search

let internal recordFields (t: Type) =
    Reflection.FSharpType.GetRecordFields t
    |> Array.map (fun x -> x.Name)

let internal searchIndex<'T> (client: ISearchIndexClient) (config: SearchConfig option) (details: QueryDetails) =
    config
    |> Option.bind (fun c -> c.Log)
    |> Option.iter (fun log -> log details)

    task {
        // Return only fields used in the return record type
        details.Parameters.Select <- recordFields (typeof<'T>)
        let! searchResult =
            client.Documents.SearchAsync<'T>(
                details.Text |> Option.toObj,
                details.Parameters,
                details.RequestOptions |> Option.toObj,
                config |> Option.bind (fun c -> c.CancellationToken) |> Option.defaultValue CancellationToken.None)

        let facets =
            searchResult.Facets
            |> Option.ofObj
            |> Option.map (
                Seq.map (fun pair -> pair.Key, pair.Value |> Seq.map (fun facet -> string facet.Value))
                >> Map)
            |> Option.defaultValue Map.empty

        return
            { Results = searchResult.Results |> Seq.map (fun r -> r.Document)
              Facets = facets
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