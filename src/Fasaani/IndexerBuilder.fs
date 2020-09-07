namespace Fasaani.Index

open System
open Microsoft.Azure.Search.Models

type IndexerFieldMapping =
    | Input of FieldMapping seq
    | Output of FieldMapping seq

[<AutoOpen>]
module IndexerBuilder =

    type IndexerDefinitionBuilder () =

        member _.Yield _ =
            { Indexer = Indexer()
              DataSource = None }

        /// Sets the indexer name
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

        /// Sets the indexer description
        [<CustomOperation"description">]
        member _.Description(state: IndexerDefinition, description) =
            state.Indexer.Description <- description
            state

        /// Sets the indexer etag
        [<CustomOperation"etag">]
        member _.ETag(state: IndexerDefinition, etag) =
            state.Indexer.ETag <- etag
            state

        /// Sets the indexer field mappings
        [<CustomOperation"fieldMappings">]
        member _.FieldMappings(state: IndexerDefinition, fieldMappings: IndexerFieldMapping) =
            match fieldMappings with
            | Input mappings -> state.Indexer.FieldMappings <- ResizeArray<FieldMapping> mappings
            | Output mappings -> state.Indexer.OutputFieldMappings <- ResizeArray<FieldMapping> mappings
            state

        /// Sets the indexer is disabled. Default value is false
        [<CustomOperation"disabled">]
        member _.Disabled(state: IndexerDefinition, isDisabled) =
            state.Indexer.IsDisabled <- Nullable isDisabled
            state

        /// Sets the indexer skill set name
        [<CustomOperation"skillSet">]
        member _.SkillSet(state: IndexerDefinition, name) =
            state.Indexer.SkillsetName <- name
            state

        /// Sets the index fields
        [<CustomOperation"dataSource">]
        member _.DataSource(state: IndexerDefinition, dataSource) =
            { state with DataSource = Some dataSource}

    let indexer = IndexerDefinitionBuilder ()