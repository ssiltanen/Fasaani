module Fasaani.Test.IntegrationTests

open System
open System.ComponentModel.DataAnnotations
open Expecto
open Microsoft.Azure.Search
open Microsoft.Azure.Search.Models
open Microsoft.Spatial
open Fasaani

[<CLIMutable>]
type AzureSearchConfig =
    { SearchName: string
      AdminKey: string
      QueryKey: string }

[<CLIMutable>]
type UsersIndex =
    { [<Key; IsFilterable>]
      Id : string
      [<IsFilterable; IsSearchable; IsFacetable; IsSortable>]
      Name : string
      [<IsFilterable; IsFacetable; IsSortable>]
      Age : int
      [<IsFilterable; IsFacetable; IsSortable>]
      HeightInMeters : float
      [<IsFilterable; IsSortable>]
      IsActive : bool
      [<IsFilterable; IsSortable>]
      Coordinate : GeographyPoint
      [<IsFilterable; IsSortable>]
      CreatedAt : DateTime }

let random = Random ()

let user modify : UsersIndex =
    let randomFloat min max = random.NextDouble() * (max - min) + min
    // Leave a wee leeway to change coordinates in tests towards the limits
    let lat = randomFloat -85.0 85.0
    let lon = randomFloat -175.0 175.0
    { Id = Guid.NewGuid() |> string
      Name = Guid.NewGuid() |> string
      Age = random.Next(100)
      HeightInMeters = randomFloat 1.0 2.0
      IsActive = true
      Coordinate = GeographyPoint.Create(lat, lon)
      CreatedAt = random.NextDouble() |> DateTime.UtcNow.AddDays }
    |> modify

let createUsers n = [1..n] |> List.map (fun _ -> user id)

let searchSyncronously client =
    searchAsync<UsersIndex> client
    >> Async.AwaitTask
    >> Async.RunSynchronously

let userId user = user.Id

let userIds users = users |> Seq.map userId

let gpsDistanceFromZero lat lon =
    let lat0 = 0.0
    let lon0 = 0.0
    let earthRadiusInMeters = 6371e3 // metres
    let fii1 = lat0 * Math.PI / 180.0 // fii, lambda in radians
    let fii2 = lat * Math.PI / 180.0
    let deltaFii = (lat - lat0) * Math.PI / 180.0
    let deltaLambda = (lon - lon0) * Math.PI / 180.0
    let a =
        Math.Sin(deltaFii / 2.0) * Math.Sin(deltaFii / 2.0)
        + Math.Cos(fii1) * Math.Cos(fii2)
        * Math.Sin(deltaLambda / 2.0) * Math.Sin(deltaLambda / 2.0)
    let c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a))
    earthRadiusInMeters * c // in metres

let testsWithSetup setup = [

    test "Filter Eq returns user with exact id" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            let user = List.head users
            users |> insert |> ignore
            let searchResults =
                query {
                    filter (where "Id" Eq user.Id)
                } |> searchSyncronously client

            "Only one user should have been found"
            |> Expect.hasLength searchResults.Results 1
            "Found user id should match the searched user id"
            |> Expect.equal user.Id (Seq.head searchResults.Results |> userId)
        )
    }

    test "Filter Ne returns all other users but one with id" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            let user = List.head users
            users |> insert |> ignore
            let searchResults =
                query {
                    filter (where "Id" Ne user.Id)
                } |> searchSyncronously client

            "All but one users should have been returned"
            |> Expect.hasLength searchResults.Results 9
            "None of the found users should have the filter Id value"
            |> Expect.all searchResults.Results (fun u -> u.Id <> user.Id)
        )
    }

    test "Filter Gt returns users greater than age" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let ageLimit = 50
            let searchResults =
                query {
                    filter (where "Age" Gt ageLimit)
                } |> searchSyncronously client
            let expectedUserIds = users |> List.choose (fun u -> if u.Age > ageLimit then Some u.Id else None)

            "All returned users should have age higher than ageLimit"
            |> Expect.all searchResults.Results (fun u -> u.Age > ageLimit)
            "All created users with age higher than ageLimit should have been returned"
            |> Expect.containsAll (userIds searchResults.Results) expectedUserIds
        )
    }

    test "Filter Lt returns users less than age" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let ageLimit = 50
            let searchResults =
                query {
                    filter (where "Age" Lt ageLimit)
                } |> searchSyncronously client
            let expectedUserIds = users |> List.choose (fun u -> if u.Age < ageLimit then Some u.Id else None)

            "All returned users should have age lower than ageLimit"
            |> Expect.all searchResults.Results (fun u -> u.Age < ageLimit)
            "All created users with age lower than ageLimit should have been returned"
            |> Expect.containsAll (userIds searchResults.Results) expectedUserIds
        )
    }

    test "Filter Ge returns users greater than or equal age" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let ageLimit = 50
            let searchResults =
                query {
                    filter (where "Age" Ge ageLimit)
                } |> searchSyncronously client
            let expectedUserIds = users |> List.choose (fun u -> if u.Age >= ageLimit then Some u.Id else None)

            "All returned users should have age higher than or equal ageLimit"
            |> Expect.all searchResults.Results (fun u -> u.Age >= ageLimit)
            "All created users with age higher than or equal ageLimit should have been returned"
            |> Expect.containsAll (userIds searchResults.Results) expectedUserIds
        )
    }

    test "Filter Le returns users less than or equal age" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let ageLimit = 50
            let searchResults =
                query {
                    filter (where "Age" Le ageLimit)
                } |> searchSyncronously client
            let expectedUserIds = users |> List.choose (fun u -> if u.Age <= ageLimit then Some u.Id else None)

            "All returned users should have age lower than or equal ageLimit"
            |> Expect.all searchResults.Results (fun u -> u.Age <= ageLimit)
            "All created users with age lower than or equal ageLimit should have been returned"
            |> Expect.containsAll (userIds searchResults.Results) expectedUserIds
        )
    }

    test "Filter with multiple and expressions should return user when all conditions match" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            let user = List.head users
            users |> insert |> ignore
            let filterExpr =
                [ where "Id" Eq user.Id
                  where "Name" Eq user.Name
                  where "Age" Eq user.Age ]
                |> combineWithAnd
            let searchResults =
                query {
                    filter filterExpr
                } |> searchSyncronously client

            "Only one user should have been found"
            |> Expect.hasLength searchResults.Results 1
            "Found user id should match the searched user id"
            |> Expect.equal user.Id (Seq.head searchResults.Results |> userId)
        )
    }

    test "Filter with multiple or expressions should return users matching any of the conditions" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            let user = List.head users
            users |> insert |> ignore
            let filterExpr =
                [ where "Id" Eq user.Id
                  where "Name" Eq user.Name
                  where "Age" Eq user.Age ]
                |> combineWithOr
            let searchResults =
                query {
                    filter filterExpr
                } |> searchSyncronously client

            "At least one user should have been found"
            |> Expect.isGreaterThanOrEqual (Seq.length searchResults.Results) 1
            "Found users should have either matching id, name or age"
            |> Expect.all searchResults.Results (fun u -> u.Id = user.Id || u.Name = user.Name || u.Age = user.Age)
        )
    }

    test "Filter with multiple and expressions combined with or expressions should return users matching any of the inner and conditions" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let userFilter user =
                [ where "Id" Eq user.Id
                  where "Name" Eq user.Name
                  where "Age" Eq user.Age ]
                |> combineWithAnd
            let filterExpr =
                users
                |> List.take 3
                |> List.map userFilter
                |> combineWithOr
            let searchResults =
                query {
                    filter filterExpr
                } |> searchSyncronously client
            let expectedUserIds =
                users
                |> List.take 3
                |> List.map userId

            "At least three user should have been found"
            |> Expect.isGreaterThanOrEqual (Seq.length searchResults.Results) 3
            "Found users should have either matching id, name or age"
            |> Expect.containsAll (userIds searchResults.Results) expectedUserIds
        )
    }

    test "Filter with Not is negated" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            let user = List.head users
            users |> insert |> ignore
            let searchResults =
                query {
                    filter (where "Id" Ne user.Id |> Not)
                } |> searchSyncronously client

            "Only one user should have been found"
            |> Expect.hasLength searchResults.Results 1
            "Negated Ne should have returned the exact match user id"
            |> Expect.equal user.Id (Seq.head searchResults.Results |> userId)
        )
    }

    test "Filter with Empty returns all users" {
        setup (fun (client : ISearchIndexClient) insert ->
            [1..10]
            |> List.map (fun _ -> user id)
            |> insert
            |> ignore
            let searchResults =
                query {
                    filter Filter.Empty
                } |> searchSyncronously client

            "All users should have been found"
            |> Expect.hasLength searchResults.Results 10
        )
    }

    test "Filter with isIn returns matching users" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            let expectedUserIds = users |> List.take 3 |> List.map userId
            users |> insert |> ignore
            let searchResults =
                query {
                    filter (whereIsIn "Id" expectedUserIds)
                } |> searchSyncronously client

            "All filtered userIds should have been found"
            |> Expect.hasLength searchResults.Results 3
            "Found users should contain the user ids of filters"
            |> Expect.containsAll (userIds searchResults.Results) expectedUserIds
        )
    }

    test "Filter with geoDistance returns all results when filter is always true" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResults =
                query {
                    filter (whereDistance "Coordinate" (Lat 0.0M, Lon 0.0M) Ge 0)
                } |> searchSyncronously client

            "All created users should have been returned"
            |> Expect.hasLength searchResults.Results 10
        )
    }

    test "Filter with geoDistance returns no results when filter is never true" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResults =
                query {
                    filter (whereDistance "Coordinate" (Lat 0.0M, Lon 0.0M) Lt 0)
                } |> searchSyncronously client

            "All created users should have been returned"
            |> Expect.isEmpty searchResults.Results
        )
    }

    test "Filter with geoIntersects returns intersecting user" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let user = List.head users
            let userLat = decimal user.Coordinate.Latitude
            let userLon = decimal user.Coordinate.Longitude
            let coordinateA = Lat (userLat + 2M), Lon (userLon - 2M)
            let coordinateB = Lat (userLat - 2M), Lon (userLon - 2M)
            let coordinateC = Lat (userLat + 2M), Lon (userLon + 2M)
            let searchResults =
                query {
                    // Notice that last coordinates must equal to the first and coordinates to be in counterclockwise order
                    filter (whereIntersects "Coordinate" [ coordinateA; coordinateB; coordinateC; coordinateA ])
                } |> searchSyncronously client

            "Expected user should have been found"
            |> Expect.contains (userIds searchResults.Results) user.Id
        )
    }

    test "Filter with geoIntersects does not return non-intersecting user" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let user = List.head users
            let userLat = decimal user.Coordinate.Latitude
            let userLon = decimal user.Coordinate.Longitude
            let coordinateA = Lat (userLat + 2M), Lon (userLon - 2M)
            let coordinateB = Lat (userLat + 1M), Lon (userLon - 2M)
            let coordinateC = Lat (userLat + 1M), Lon (userLon - 1M)
            let searchResults =
                query {
                    // Notice that last coordinates must equal to the first and coordinates to be in counterclockwise order
                    filter (whereIntersects "Coordinate" [ coordinateA; coordinateB; coordinateC; coordinateA ])
                } |> searchSyncronously client
            let foundUserById = searchResults.Results |> Seq.tryFind (fun u -> u.Id = user.Id)

            "User should not have been found"
            |> Expect.isNone foundUserById
        )
    }

    test "Total result count is returned when it is specified in Query" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResult =
                query {
                    searchText "*"
                    includeTotalResultCount
                } |> searchSyncronously client

            let resultCount =
                "Count in results should have been Some when includeTotalResultCount is specified"
                |> Expect.wantSome searchResult.Count
            "Total count should have been 10"
            |> Expect.equal resultCount 10L
        )
    }

    test "Total result count is not returned when it is not specified in Query" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResult =
                query {
                    searchText "*"
                } |> searchSyncronously client

            "Count in results should have been None when includeTotalResultCount is not specified"
            |> Expect.isNone searchResult.Count
        )
    }

    test "Order byField returns users in order" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResult =
                query {
                    searchText "*"
                    order [ byField "Age" Desc ]
                } |> searchSyncronously client

            "Found users should have been in Age descending order"
            |> Expect.isDescending (searchResult.Results |> Seq.map (fun u -> u.Age))
        )
    }

    test "Order bySearchScore returns users in order" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResults =
                query {
                    searchText "*"
                    order [ byDistance "Coordinate" (Lat 0M, Lon 0M) Desc ]
                } |> searchSyncronously client

            let expectedUsersInOrder =
                users
                |> List.sortByDescending (fun u -> gpsDistanceFromZero u.Coordinate.Latitude u.Coordinate.Longitude)
            "Found users should be in descending distance order from zero coordinate"
            |> Expect.sequenceEqual (userIds searchResults.Results) (userIds expectedUsersInOrder)
        )
    }

    test "Skip in Query skips first n results" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResults =
                query {
                    searchText "*"
                    skip 5
                } |> searchSyncronously client

            "After skipping 5, 5 returns should be returned"
            |> Expect.hasLength searchResults.Results 5
        )
    }

    test "Top in Query limits the count of returned results" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResults =
                query {
                    searchText "*"
                    top 5
                } |> searchSyncronously client

            "Only 5 users should have been returned after limiting results count"
            |> Expect.hasLength searchResults.Results 5
        )
    }

    test "Facets in Query returns facets" {
        setup (fun (client : ISearchIndexClient) insert ->
            let users = createUsers 10
            users |> insert |> ignore
            let searchResults =
                query {
                    searchText "*"
                    facets [ "Name"; "Age" ]
                } |> searchSyncronously client

            let uniqueNames = users |> List.map (fun u -> u.Name) |> List.distinct |> List.sort
            let uniqueAges = users |> List.map (fun u -> u.Age) |> List.distinct |> List.sort

            let foundNameFacets = searchResults.Facets.Item "Name" |> Seq.sort
            let foundAgeFacets = searchResults.Facets.Item "Age" |> Seq.map int |> Seq.sort

            "Name facets should match unique names created"
            |> Expect.sequenceEqual foundNameFacets uniqueNames
            "Age facets should match unique ages created"
            |> Expect.sequenceEqual foundAgeFacets uniqueAges
        )
    }

]

(*
Setup functions to setup and clean up for each test above
*)

let insertDataIntoIndex(client : ISearchIndexClient) documents =
    documents
    |> List.map (fun (doc : 'a) -> IndexAction.Upload(doc))
    |> IndexBatch.New
    |> client.Documents.Index
    |> fun indexingResult ->
        // Not proud of this hack but, indexing seem to need some time before documents are queryable
        Async.Sleep 1500 |> Async.RunSynchronously
        seq indexingResult.Results

let tests (config : AzureSearchConfig) =
    testsWithSetup (fun test ->
        // For each test
        // create index, client management and querying client
        // at the end destroy index
        use serviceClient = new SearchServiceClient (config.SearchName, SearchCredentials (config.AdminKey)) :> ISearchServiceClient
        let index = Index()
        index.Name <- "users"
        index.Fields <- FieldBuilder.BuildForType<UsersIndex>()
        try
            serviceClient.Indexes.CreateOrUpdate index |> ignore
            use indexClient = new SearchIndexClient (config.SearchName, index.Name, SearchCredentials(config.QueryKey)) :> ISearchIndexClient
            use insertClient = serviceClient.Indexes.GetClient index.Name
            test indexClient (insertDataIntoIndex insertClient)
        finally
            if serviceClient.Indexes.Exists(index.Name) then
                serviceClient.Indexes.Delete(index.Name)
    )
    |> testList "Integration test"