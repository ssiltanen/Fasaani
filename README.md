# Fasaani

<p align="center">
<img src="https://github.com/ssiltanen/Fasaani/raw/master/Logo.png" width="200px"/>
</p>

Fasaani is a simple F# wrapper on top of Azure Search .NET SDK. It does not try to cover all functionality of the wrapped SDK, but instead it covers some common use cases of querying Azure Search indexes and offers an easy way to use the raw SDK underneath while still using the nicer syntax of Fasaani.

## Installation

Download the assembly from Nuget [https://www.nuget.org/packages/Fasaani](https://www.nuget.org/packages/Fasaani) and open namespace `Fasaani` in your code.

```fsharp
open Fasaani
```

## Overview of supported query settings

```fsharp
query {
    searchText "text used to search"                // Text searched from index
    searchFields [ "field" ]                        // Which fields are matched with text search
    searchMode All                                  // Whether any or all of the search terms must be matched. All or Any
    querySyntax Simple                              // Configure query to use simple syntax or Lucene syntax. Simple or Lucene
    facets [ "field"; "field2" ]                    // Facets to return, see facets
    filter (where "field" Eq 1)                     // Query filter, see filtering
    skip 10                                         // Skip count of results, see paging
    top 25                                          // How many results are returned, see paging
    order [ byField "field2" Asc ]                  // Order of returned results, see paging
    includeTotalResultCount                         // Return total matched results count, see paging
    highlightFields [ "field1"; "field2" ]          // Fields to use for hit highlighting
    highlightTags (Pre "&amp;") (Post "&amp//;")    // Tags to prepend and append highlight hits
    minCoverage 90.                                 // Index search minimum coverage between 0 and 100
    scoringProfile "profile1"                       // Name of profile to evaluate match score to sort results
    scoringParameters [                             // Profile values to use in scoring functions
        "profile1", Values [ "value1"; "value2" ]
        "profile2", GeoPoint (Lat -122.2M, Lon 44.8M) 
    ]
    clientRequestId (Guid.NewGuid() |> Some)        // Request tracking id for debug purposes
} |> searchAsync<'T> indexClient
```

**All above settings are optional so use only the ones you need in your query.** More info of each can be found below. 

Extra mention also to Select parameter that sets the returned fields from result documents in the underlying library which is implicitly implemented in Fasaani from the output model properties.

There are synchronous and asynchronous version of each search function, but as async versions are highly recommended for actual use, the examples here are done using the async versions of these functions.

## How to use

### Basic query

First create SearchIndexClient by calling `searchIndexClient` function. Notice that you should use the secondary key as it has less privileges and don't forget to `use` since the returned ISearchIndexClient implements IDisposable interface.

```fsharp
use indexClient = searchIndexClient searchName searchQueryKey indexName
```

After creating the client, define a basic text search query with Fasaani `query` computational expression, and pipe it to `searchAsync` expression to execute your query:

```fsharp
query {
    searchText "text used to search"
} |> searchAsync<'T> indexClient

// Returns
// type SearchResult<'T> =
//     { Results: 'T seq
//       Facets: Map<string, string seq>
//       Count: int64 option
//       Raw: DocumentSearchResult<'T> }
```

Notice that you need to provide `searchAsync` expression with a type parameter to determine what type of results is returned.

In the response model, `Raw` contains the full as-is response the underlying SDK returned. This is returned to make sure that using this library does not prevent some use cases that are not pre-parsed for the user. The other three fields `Results`, `Facets`, and `Count` are pre-parsed from the underlying response for easier access in F#.

### Query with filtering

Fasaani offers simple way to create filters for your queries. The easiest way to create a filter is to use the `where` function:

```fsharp
let filter = where "MyField" Eq "Value"
let filter2 = where "NumericField" Gt 5
let filter3 = where "MyField" Eq null
// etc..
```

The supported comparison operators are `Eq`, `Ne`, `Gt`, `Lt`, `Ge`, and `Le`. 

If Fasaani does not support a type of filter you need, it is also possible to create your raw OData filter:

```fsharp
Filter.OData "MyField eq 'Value'"
```

To combine filters into multi-filter expression, Fasaani has two helper functions: `combineWithAnd`, and `combineWithOr`. For example:

```fsharp
[ where "MyField" Eq "Value"
  where "AnotherField" Eq "AnotherValue"
  where "NumericField" Gt 5 ]
|> combineWithAnd

[ where "MyField" Eq "Value"
  where "AnotherField" Eq "AnotherValue"
  where "NumericField" Gt 5 ]
|> combineWithOr
```

If you want more freedom over how these are combined you can also combine them by folding. Filters have two custom operators `(+)` = and, and `(*)` = or, which can be used to combine filters:

```fsharp

[ where "MyField" Eq "Value"
  where "AnotherField" Eq "AnotherValue"
  where "NumericField" Gt 5 ]
|> List.fold (+) Filter.Empty

// Or withot fold
(where "MyField" Eq "Value") + (where "AnotherField" Eq "AnotherValue")
```

It is also possible to create a filter for OData `search.in` function with helpers `whereIsIn` and `whereIsInDelimited`:

```fsharp
[ whereIsIn "MyField" [ "Value1"; "Value2" ]
  whereIsInDelimited "AnotherField" [ "Value with spaces and ,"; "Another value" ] '|' ] // Just an example character as pipe
|> combineWithAnd
```

To negate the filter you can use either `isNot` or operator `(!!)`

```fsharp
where "MyField" Eq "Value" |> isNot
// or
!! (where "MyField" Eq "Value")
```

After forming the filter, provide it to the `query` CE:

```fsharp
let filterExpr =
    [ where "MyField" Eq "Value"
      where "AnotherField" Eq "AnotherValue"
      where "NumericField" Gt 5 ]
    |> List.fold (+) Filter.Empty

query {
    filter filterExpr
} |> searchAsync<'T> indexClient

// Or with some text search
query {
    searchText "Some text to search with"
    filter filterExpr
} |> searchAsync<'T> indexClient
```

#### Geo functions

Fasaani supports filtering with OData geo functions geo.distance and geo.intersect with functions `whereDIstance` and `whereIntersects`. They can be used together with `where` function or alone as a filter

```fsharp
let distanceFilter = whereDistance "field" (Lat -122.131577M, Lon 47.678581M) Lt 1

let intersectFilter =
    whereIntersects 
        "field"
        [ Lat -122.031577M, Lon 47.578581M
          Lat -122.031577M, Lon 47.678581M
          Lat -122.131577M, Lon 47.678581M
          Lat -122.031577M, Lon 47.578581M ]
```

Notice in the whereIntersects example that Azure Search has rules for the coordinate sequence:
 
- At least 3 unique coordinate points
- Points in a polygon must be in counterclockwise order
- The polygon needs to be closed, meaning the first and last point sets must be the same.

For now Fasaani does not provide any automatisation for these, but they are planned for the future.

### Paging and order by

Azure search incorporates a default skip and top values. Currently skip = 0 and top 50. These values can be overwritten with Fasaani with `skip`, and `top` operators.

Order by fields are specified with a collection of either field based order, distance based order or search score based order.

By default Azure Search does not calculate the total count of found documents for the search, and it needs to be specified explicitly with `includeTotalResultCount` operator:

```fsharp
query {
    searchText "Search text"
    skip 0
    top 100
    order [ byField "field1" Asc; byDistance "field2" (Lat -122.131577M, Lon 47.678581M) Desc; bySearchScore Desc ]
    includeTotalResultCount
} |> searchAsync<'T> indexClient
```

Skip value cannot be greater than 100 000. If you need to scan documents in sequence, but cannot use skip due to this limitation, consider using orderby on a totally-ordered key and $ilter with a range query instead.

If results are truncated due to server-side paging, Azure Cognitive Search can't include all results in a single response, the response returned will include a continuation token that can be passed to ContinueSearch to retrieve more results. See DocumentSearchResult.ContinuationToken for more information. This continuation token can be found in the Raw results.

Continuation token can be used with `continueSearchAsync` function:

```fsharp
let requestId = Guid.NewGuid() // Imagine here a value used in previous query
continueSearchAsync<'T> indexClient (Some requestId) continuationToken
```

Note that this method is not meant to help you implement paging of search results. You can implement paging using the `top` and `skip`. 

### Facets

Specify collection of facets to return them in the response

```fsharp
query {
    searchText "Search text"
    facets [ "field1"; "field2" ] // Response Facets are empty unless these are provided
} |> searchAsync<'T> indexClient
```

In the return value, facets are returned as a `Map<string, string seq>` where keys are the field names specified in the query facets above (in this case field1 and field2). and payload contains unique values of that field.

### Search mode

To specify whether any or all of the search terms must match in order to count the document as a match. Possible values include: `Any`, `All`

```fsharp
query {
    searchText "Search text"
    searchMode All
} |> searchAsync<'T> indexClient
```

### Search query syntax aka query mode

To specify query mode i.e. the syntax used in the query, provide querySyntax value of possible values `Simple`, `Lucene`.

```fsharp
query {
    searchText "Search text"
    querySyntax Lucene
} |> searchAsync<'T> indexClient
```

### Select

Azure Search .NET SDK allows setting select fields aka fields that are returned of found documents. Using this parameter is not necessary in Fasaani, as Fasaani uses reflection on the type parameter provided and sets them as query select field values.

### Search fields

To limit which fields Azure Search uses for text search, provide search fields in a collection. When no search fields are specified, all document fields are searched.

```fsharp
query {
    searchText "Search text"
    searchFields [ "field1"; "field2" ]
} |> searchAsync<'T> indexClient
```

### Special constants

Fasaani maps F# float special values (`nan`, `infinity`, `-infinity`) to OData counterparts (NaN, INF, -INF)

These can be used in filters for example:

```fsharp
let filter =
    [ where "field1" Lt infinity
      where "field2" Gt -infinity
      where "field3" Ne nan ]
```

### Query configuration

So far in each example we have used the `searchAsync` function. This function is the non-configurable version of search function. To customize how the search is executed, use `SearchWithConfigAsync`, which can be configured to logger function and/or cancellation token.

Logging with config record works by creating a record of `SearchConfig` with `Log` value. This field is a function that is called before query execution if it is provided. For now it is user's responsibility to provide the implementation for that function in a form that pleases them.

Cancellation token works in the same fashion as logging. If `CancellationToken` is provided with `SearchConfig` record, it is passed to the underlying SDK. Otherwise CancellationToken.None is passed.

Fasaani offers a simple `defaultConfig` function that accepts a logger method of (string -> unit) to log query details. This function uses QueryDetails.ToString static method under the hoods to print the settings used in query.

For Example:

```fsharp
// Type specified here for documentation purposes
type SearchConfig =
    { Log: (QueryDetails -> unit) option
      CancellationToken: CancellationToken option }

// Example of using default configs
let configWithLogging = defaultConfig log.LogInformation // Logging provider not specified here

// Example of how to specify the config record with your custom evaluator
let configWithLogging =
    { Log =
        fun details ->
            details.Text |> Option.iter log.LogInformation // Logging provider not specified here
            details.Parameters.Filter |> Option.ofObj |> Option.iter log.LogInformation
        |> Some
      CancellationToken = None }

query {
    searchText "Search text"
    filter (where "MyField" Eq "Bear")
} |> searchWithConfigAsync<'T> indexClient configWithLogging
```

## Azure Search Clients

Azure Search SDK has two client classes and interfaces for them: `ISearchServiceIndex` and `ISearchIndexClient`.

`ISearchServiceClient` is used to manage the Search service that hosts all the indices, eg. create and destroy indices and indexers.

`ISearchIndexClient` is used to query a certain index and/or to insert data into the index.

When creating these clients it is important to notice that when Azure provides two keys to access the Search service: `Admin` and `Query` keys. Admin key is needed to create the `ISearchServiceIndex` and when inserting/updating/deleting data from index with `ISearchIndexClient`.

To make it easier to create these clients, Fasaani implements `searchServiceClient`, `searchIndexClient`, and `searchIndexAdminClient` functions.

```fsharp
let adminKey = "..."
let queryKey = "..."

// Create service client
use serviceClient = searchServiceClient "mySearch" adminKey

// It is recommended to create index client using query key if you dont need to
// mutate data in the index
use indexClient = searchIndexClient "mySearch" queryKey "myIndex"

// Another way to create index client is to create it with service client,
// however this index client uses admin privileges.
use indexAdminClient = searchIndexAdminClient serviceClient "myIndex"
```

## Index and Indexer

It is possible to create indices and indexers with Fasaani syntax. For that purpose, Fasaani has computation expressions `index` and `indexer`.

```fsharp
open Fasaani.Index

// Service client with admin key is needed
let client = searchServiceClient "search-name" "admin-key"

// Define data source as you would normally
let source = DataSource.AzureBlobStorage("connection-name", connectionString, "container")

let indexer1 =
    indexer {
        name "indexer1"
        interval (TimeSpan.FromHours 1.)
        dataSource (CreateOrUpdate source)
        parameters (IndexingParameters().ParseJsonArrays())
    }

// When creating indexer with existing data source, name of the data source is then enough
let indexer2 =
    indexer {
        name "indexer2"
        startTime (DateTimeOffset())
        interval (TimeSpan.FromHours 2.)
        dataSource (UseExisting "connection-name")
        parameters (IndexingParameters().ParseJsonArrays())
    }

// Creating index with indexers creates all of them on one stroke
index {
    name "example-index"
    fields (fieldsOfType<MyType>())
    indexers [ indexer1; indexer2 ]
} |> createOrUpdateIndexAsync client

// However it is possible to create indexers separately also
indexer1 |> createOrUpdateIndexerAsync client "example-index"

// Or if you want to validate index before create call
index |> validateIndex |> Result.map (createOrUpdateIndexAsync client)
```

Other possible settings for the Index CE include `analyzers`, `charFilters`, `cors`, `defaultScoringProfile`, `etag`, `scoringProfiles`, `suggesters`, `tokenFilters`, and `tokenizers`. They are operated the same way as the original library.

Other possible settings for the Indexer CE include `description`, `etag`, `fieldMappings`, `disabled`, and `skillSet`.

## Contributions

When contributing to this repository, please first discuss the change you wish to make via an open issue before submitting a pull request. For new feature requests please describe your idea in more detail and how it could benefit other users as well. Please be aware that Fasaani aims to be a light, thin abstraction over the Azure SDK.

After your change has been approved, please submit your PR against `dev` branch.

Use existing code as a guideline for code conventions and keep documentation as well as tests up-to-date.