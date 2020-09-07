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
        member _.Fields(state: IndexDefinition, fields: Field seq) =
            state.Index.Fields <- ResizeArray<Field> fields
            state

        /// Sets the index analyzers
        [<CustomOperation"analyzers">]
        member _.Analyzers(state: IndexDefinition, analyzers: Analyzer seq) =
            state.Index.Analyzers <- ResizeArray<Analyzer> analyzers
            state

        /// Sets the index char filters
        [<CustomOperation"charFilters">]
        member _.CharFilters(state: IndexDefinition, charFilters: CharFilter seq) =
            state.Index.CharFilters <- ResizeArray<CharFilter> charFilters
            state

        /// Sets the index cors options
        [<CustomOperation"cors">]
        member _.Cors(state: IndexDefinition, cors) =
            state.Index.CorsOptions <- cors
            state

        /// Sets the index default scoring profile. If this property is not set and no scoring profile is specified in the query, then default scoring (tf-idf) will be used.
        [<CustomOperation"defaultScoringProfile">]
        member _.DefaultScoringProfile(state: IndexDefinition, profile) =
            state.Index.DefaultScoringProfile <- profile
            state

        /// Sets the index etag
        [<CustomOperation"etag">]
        member _.ETag(state: IndexDefinition, etag) =
            state.Index.ETag <- etag
            state

        /// Sets the index scoring profiles
        [<CustomOperation"scoringProfiles">]
        member _.ScoringProfiles(state: IndexDefinition, profiles: ScoringProfile seq) =
            state.Index.ScoringProfiles <- ResizeArray<ScoringProfile> profiles
            state

        /// Sets the index suggesters
        [<CustomOperation"suggesters">]
        member _.Suggesters(state: IndexDefinition, suggesters: Suggester seq) =
            state.Index.Suggesters <- ResizeArray<Suggester> suggesters
            state

        /// Sets the index token filters
        [<CustomOperation"tokenFilters">]
        member _.TokenFilters(state: IndexDefinition, filters: TokenFilter seq) =
            state.Index.TokenFilters <- ResizeArray<TokenFilter> filters
            state

        /// Sets the index tokenizers
        [<CustomOperation"tokenizers">]
        member _.Tokenizers(state: IndexDefinition, tokenizers: Tokenizer seq) =
            state.Index.Tokenizers <- ResizeArray<Tokenizer> tokenizers
            state

        /// Sets indexer
        [<CustomOperation"indexers">]
        member _.Indexers(state: IndexDefinition, indexers) =
            { state with Indexers = Seq.toList indexers }

    let index = IndexDefinitionBuilder ()