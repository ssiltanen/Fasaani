# Fasaani

<p align="center">
<img src="https://github.com/ssiltanen/Fasaani/raw/master/Logo.png" width="200px"/>
</p>

Fasaani is a simple F# wrapper on top of Azure Search .NET SDK. This library does not try to cover all functionality of the wrapped SDK, but instead it covers some common use cases of querying Azure Search indexes.

## Installation

For now, this library is still in its early stages, and its not ready for real usage. Therefore, no installation guides yet.

## How to use

### All settings in one

```fsharp
search {
    searchText "text used to search"    // text searched from index
    searchFields [ "SomeField" ]        // Which fields are matched with text search
    searchMode All                      // whether any or all of the search terms must be matched. All or Any
    querySyntax Simple                  // configure query to use simple syntax or Lucene syntax. Simple or Lucene
    facets [ "facet1"; "facet2" ]       // Facets to return
    filter (where "field" Eq 1)         // Query filter, see more below
    skip 10                             // Skip count of results, see paging
    top 25                              // How many results are returned, see paging
    includeTotalResultCount             // Return total matched results count
    requestOptions opt                  // Raw request options of underlying SDK
    //parameters par                    // Raw parameters of underlying SDK. Overwrites above other settings!
} |> searchAsync<MyModel> indexClient
```

All above settings are optional so use only the ones you need in your query. More info of each can be found below. The parameters setting is commented out in the example to emphasize that it should not be used together with other settings as they overwrite each other if used in the same query.

### Basic query

First create SearchIndexClient like you normally would. Notice that you should use the secondary key as it has less privileges.

```fsharp
use indexClient = new SearchIndexClient (searchName, indexName, SearchCredentials(searchQueryKey)) :> ISearchIndexClient
```

After creating the client, define a basic text search query with Fasaani `search` computational expression, and pipe it to `searchAsync` expression to execute your query:

```fsharp
search {
    searchText "text used to search"
} |> searchAsync<MyModel> indexClient

// Returns
// type SearchResult<'T> =
//     { Results: 'T seq
//       Facets: Map<string, string seq>
//       Count: int64 option
//       Raw: DocumentSearchResult<'T> }
```

Notice that you need to provide `searchAsync` expression with your type (here MyModel) to determine what type of results is returned.

In the response model, `Raw` contains the full as-is response the underlying SDK returned. This is returned to make sure that using this library does not prevent some use cases that are not pre-parsed for the user. The other three fields `Results`, `Facets`, and `Count` are pre-parsed from the underlying response for easier access in F#.

### Query with filtering

Fasaani offers simple way to create filters for your queries. The easiest way to create a filter is to use the `where` function:

```fsharp
let filter = where "MyField" Eq "Value"
let filter2 = where "NumericField" Gt 5
let filter3 = where "MyField" Eq null
// etc..
```

The supported comparison operators are `Eq`, `Ne`, `Gt`, `Lt`, `Ge`, and `Le`. It is also possible to create your raw OData filter:

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

After forming the filter, provide it to the search CE:

```fsharp
let filterExpr =
    [ where "MyField" Eq "Value"
      where "AnotherField" Eq "AnotherValue"
      where "NumericField" Gt 5 ]
    |> List.fold (+) Filter.Empty

search {
    filter filterExpr
} |> searchAsync<MyModel> indexClient

// Or with some text search
search {
    searchText "Some text to search with"
    filter filterExpr
} |> searchAsync<MyModel> indexClient
```

#### Geo functions

Fasaani supports filtering with OData geo functions geo.distance and geo.intersect with functions `whereDIstance` and `whereIntersects`. They can be used together with `where` function or alone as a filter

```fsharp
let distanceFilter = whereDistance "field" (Lat -122.131577M, Lon 47.678581M) Lt 1

let intersectFilter =
    whereIntersects 
        "field" 
        (Lat -122.031577M, Lon 47.578581M)
        (Lat -122.031577M, Lon 47.678581M)
        (Lat -122.131577M, Lon 47.678581M)
        (Lat -122.031577M, Lon 47.578581M)
```

### Paging and order by

Azure search incorporates a default skip and top values. Currently skip = 0 and top 50. These values can be overwritten with Fasaani with `skip`, and `top` operators.

Order by fields are specified with a collection of either field based order, distance based order or search score based order.

By default Azure Search does not calculate the total count of found documents for the search, and it needs to be specified explicitly with `includeTotalResultCount` operator:

```fsharp
search {
    searchText "Search text"
    skip 0
    top 100
    order [ byField "field1" Asc; byDistance "field2" -122.131577M 47.678581M Desc; bySearchScore Desc ]
    includeTotalResultCount
} |> searchAsync<MyModel> indexClient
```

### Facets

Specify collection of facets to return them in the response

```fsharp
search {
    searchText "Search text"
    facets [ "Facet1"; "Facet2" ] // Response Facets are empty unless these are provided
} |> searchAsync<MyModel> indexClient
```

### Search mode

To specify whether any or all of the search terms must match in order to count the document as a match. Possible values include: Any, All

```fsharp
search {
    searchText "Search text"
    searchMode All
} |> searchAsync<MyModel> indexClient
```

### Search query syntax aka query mode

To specify query mode i.e. the syntax used in the query, provide querySyntax value of possible values Simple, Lucene.

```fsharp
search {
    searchText "Search text"
    querySyntax Lucene
} |> searchAsync<MyModel> indexClient
```

### Select

Azure Search .NET SDK allows setting select fields aka fields that are returned of found documents. Using this parameter is not necessary in Fasaani, as Fasaani uses reflection on the type parameter provided and sets them as query select field values.

### Search fields

To limit which fields Azure Search uses for text search, provide search fields in a collection. When no search fields are specified, all document fields are searched.

```fsharp
search {
    searchText "Search text"
    searchFields [ "field1"; "field2" ]
} |> searchAsync<MyModel> indexClient
```

### Special constants

Fasaani includes some constants used in OData: NaN, Infinite, NegativeInfinite.

These can be used in filters:

```fsharp
let filter =
    [ where "field1" Lt Infinite
      where "field2" Gt NegativeInfinite
      where "field3" Ne NaN ]
```

### Query configuration

So far in each example we have the `searchAsync` function. This function is the non configurable version of `SearchWithConfigAsync`, which can be configured to logger function and/or cancellation token.

Logging with config record works by creating a record of `SearchConfig` with `Log` value. This field is a function that is called before query execution if it is provided. For now it is user's responsibility to provide the implementation for that function in a form that pleases them.

Cancellation token works in the same fashion as logging. If `CancellationToken` is provided with `SearchConfig` record, it is passed to the underlying SDK. Otherwise CancellationToken.None is passed.

For Example:

```fsharp
type SearchConfig =
    { Log: (string option -> SearchParameters -> SearchRequestOptions option -> unit) option
      CancellationToken: CancellationToken option }

let configWithLogging =
    { Log = 
        fun str parameters options ->
            str |> Option.iter log.LogInformation // Logging provider not specified here
            parameters.Filter |> Option.ofObj |> Option.iter log.LogInformation
      CancellationToken = None }

search {
    searchText "Search text"
    filter (where "MyField" Eq "Bear")
} |> searchWithConfigAsync<MyModel> indexClient configWithLogging
```

## Help! Fasaani does not support my use case

To mitigate the small set of features in Fasaani, it is possible to overwrite search parameters, and search request options of the query with `parameters`, and `requestOptions` operators, while still using the Fasaani query syntax:

```fsharp
let customParameters = SearchParameters()
customParameters.Top <- Nullable 100
let customRequestOptions = SearchRequestOptions()

search {
    searchText "Search text"
    parameters customParameters
    requestOptions customRequestOptions
} |> searchAsync<MyModel> indexClient
```

In addition, the raw `DocumentSearchResult<'T>` object from underlying method is also returned as `Raw` in the response body.

## TODO

Tests
OrderBy
Other querying features and helpers