namespace Fasaani.Index

open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.Azure.Search
open Microsoft.Azure.Search.Models

[<AutoOpen>]
module Operation =

    type DataSourceDefinition =
        | UseExisting of name : string
        | CreateOrUpdate of DataSource

    type IndexerDefinition =
        { Indexer : Indexer
          DataSource : DataSourceDefinition option }

    type IndexDefinition =
        { Index : Index
          Indexers : IndexerDefinition list }

    let fieldsOfType<'T> () = FieldBuilder.BuildForType<'T>()

    let createOrUpdateIndexerAsync (client : ISearchServiceClient) (indexName : string) (indexerDefinition : IndexerDefinition) =
        task {
            let! dataSource =
                match indexerDefinition.DataSource with
                | Some (UseExisting name) -> Task.FromResult name
                | Some (CreateOrUpdate dataSource) ->
                    task {
                        let! _ = client.DataSources.CreateOrUpdateAsync dataSource
                        return dataSource.Name
                    }
                | None ->
                    failwith "Cannot create indexer without a data source"
            indexerDefinition.Indexer.TargetIndexName <- indexName
            indexerDefinition.Indexer.DataSourceName <- dataSource
            return! client.Indexers.CreateOrUpdateAsync indexerDefinition.Indexer
        }

    let createOrUpdateIndexer client indexName indexerDefinition =
        createOrUpdateIndexerAsync client indexName indexerDefinition
        |> Async.AwaitTask |> Async.RunSynchronously

    let createOrUpdateIndexAsync (client : ISearchServiceClient) (indexDefinition : IndexDefinition) =
        task {
            let! index = client.Indexes.CreateOrUpdateAsync indexDefinition.Index
            let! indexers =
                indexDefinition.Indexers
                |> List.map (createOrUpdateIndexerAsync client indexDefinition.Index.Name)
                |> Task.WhenAll
            return index, indexers
        }

    let createOrUpdateIndex client indexDefinition =
        createOrUpdateIndexAsync client indexDefinition
        |> Async.AwaitTask |> Async.RunSynchronously