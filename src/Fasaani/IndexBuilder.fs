namespace Fasaani.Index

open Microsoft.Azure.Search.Models

[<AutoOpen>]
module IndexBuilder =

    type IndexDefinitionBuilder () =

        member _.Yield _ =
            { Index = Index()
              Indexers = [] }

        /// Sets the index name
        [<CustomOperation"name">]
        member _.Name(state: IndexDefinition, name) =
            state.Index.Name <- name
            state

        /// Sets the index fields
        [<CustomOperation"fields">]
        member _.Fields(state: IndexDefinition, fields) =
            state.Index.Fields <- fields
            state

        /// Sets indexer
        [<CustomOperation"indexers">]
        member _.Indexers(state: IndexDefinition, indexers) =
            { state with Indexers = Seq.toList indexers }

        /// Overwrites index settings
        [<CustomOperation"overWriteWith">]
        member _.OverWriteWith(state: IndexDefinition, index) =
            { state with Index = index }

    let index = IndexDefinitionBuilder ()