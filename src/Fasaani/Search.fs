[<AutoOpen>]
module Fasaani.Search

open System.Threading
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Azure.Search

let searchAsync<'T when 'T: not struct> (client: ISearchIndexClient) (config: SearchConfig option) (details: SearchDetails) =
    config
    |> Option.bind (fun c -> c.Log)
    |> Option.iter (fun log -> log details.Text details.Parameters details.RequestOptions)

    task {
        let! searchResult =
            client.Documents.SearchAsync<'T>(
                details.Text |> Option.toObj,
                details.Parameters,
                details.RequestOptions |> Option.toObj,
                config |> Option.bind (fun c -> c.CancellationToken) |> Option.defaultValue (CancellationToken.None))

        let results = searchResult.Results |> Seq.map (fun r -> r.Document)
        let count = searchResult.Count |> Option.ofNullable
        let facets =
            searchResult.Facets
            |> Option.ofObj
            |> Option.map (
                Seq.map (fun pair -> pair.Key, pair.Value |> Seq.map (fun facet -> string facet.Value))
                >> Map)
            |> Option.defaultValue Map.empty

        return
            { Results = results
              Facets = facets
              Count = count
              Raw = searchResult }
    }