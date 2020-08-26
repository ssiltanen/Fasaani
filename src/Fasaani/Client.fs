[<AutoOpen>]
module Fasaani.Client

open Microsoft.Azure.Search

let searchServiceClient searchName adminKey =
    new SearchServiceClient (searchName, SearchCredentials(adminKey)) :> ISearchServiceClient

let searchIndexClient searchName queryKey indexName =
    new SearchIndexClient (searchName, indexName, SearchCredentials(queryKey)) :> ISearchIndexClient

let searchIndexAdminClient (serviceClient : ISearchServiceClient) indexName =
    serviceClient.Indexes.GetClient indexName