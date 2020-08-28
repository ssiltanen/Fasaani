namespace Fasaani.Index

open System
open Microsoft.Azure.Search.Models

[<AutoOpen>]
module IndexerBuilder =

    type IndexerDefinitionBuilder () =

        member _.Yield _ =
            { Indexer = Indexer()
              DataSource = None }

        /// Sets the index name
        [<CustomOperation"name">]
        member _.Name(state: IndexerDefinition, name) =
            state.Indexer.Name <- name
            state

        /// Sets the indexer start time
        [<CustomOperation"startTime">]
        member _.StartTime(state: IndexerDefinition, startTime) =
            let schedule =
                Option.ofObj state.Indexer.Schedule
                |> Option.map (fun schedule ->
                    schedule.StartTime <- Nullable startTime
                    schedule)
                |> Option.defaultWith (fun _ ->
                    let schedule = IndexingSchedule()
                    schedule.StartTime <- Nullable startTime
                    schedule)
            state.Indexer.Schedule <- schedule
            state

        /// Sets the indexer interval
        [<CustomOperation"interval">]
        member _.Interval(state: IndexerDefinition, interval) =
            let schedule =
                Option.ofObj state.Indexer.Schedule
                |> Option.map (fun schedule ->
                    schedule.Interval <- interval
                    schedule)
                |> Option.defaultWith (fun _ -> IndexingSchedule(interval))
            state.Indexer.Schedule <- schedule
            state

        /// Sets the indexer parameters
        [<CustomOperation"parameters">]
        member _.Parameters(state: IndexerDefinition, parameters) =
            state.Indexer.Parameters <- parameters
            state

        /// Sets the index fields
        [<CustomOperation"dataSource">]
        member _.DataSource(state: IndexerDefinition, dataSource) =
            { state with DataSource = Some dataSource}

        /// Overwrites index settings
        [<CustomOperation"overWriteWith">]
        member _.OverWriteWith(state: IndexerDefinition, indexer) =
            { state with Indexer = indexer }

    let indexer = IndexerDefinitionBuilder ()