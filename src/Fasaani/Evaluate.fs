[<AutoOpen>]
module Fasaani.Evaluate

/// Filter helpers

let where field comparison value =
    Field (field, comparison, value)

let isNot filter =
    Not filter

let combineWithAnd : Filter seq -> Filter =
    Seq.fold (+) Filter.Empty

let combineWithOr : Filter seq -> Filter =
    Seq.fold (*) Filter.Empty

let whereDistance field coordinate comparison value =
    GeoDistance (field, coordinate, comparison, value)

let whereIntersects field a b c d =
    GeoIntersect(field, Polygon (a, b, c, d))

/// Order by helpers

let byField field direction =
    ByField (field, direction)

let byDistance field coordinate direction =
    ByGeoDistance (field, coordinate, direction)

let bySearchScore direction =
    BySearchScore direction

[<RequireQualifiedAccess>]
module OData =

    let geoDistance (field: string) (coordinate: Coordinate) =
        let (Lat lat, Lon lon) = coordinate
        sprintf "geo.distance(%s, geography'POINT(%M %M)')" field lat lon

    let geoIntersect (field: string) (polygon: Polygon) =
        let a, b, c, d = polygon
        let (Lat latA, Lon lonA) = a
        let (Lat latB, Lon lonB) = b
        let (Lat latC, Lon lonC) = c
        let (Lat latD, Lon lonD) = d
        sprintf "geo.intersects(%s, geography'POLYGON((%M %M, %M %M, %M %M, %M %M))')" field latA lonA latB lonB latC lonC latD lonD

[<RequireQualifiedAccess>]
module Filter =

    let evaluate filter =
        let rec eval = function
            | Empty -> None
            | Field (field, comparison, value) ->
                match value with
                | null -> sprintf "%s %s null" field comparison.LowerCaseValue
                | :? string as str -> sprintf "%s %s '%s'" field comparison.LowerCaseValue str
                | value -> sprintf "%s %s %O" field comparison.LowerCaseValue value
                |> Some
            | GeoDistance (field, coordinate, comparison, value) ->
                let distanceOData = OData.geoDistance field coordinate
                sprintf "%s %s %O" distanceOData comparison.LowerCaseValue value
                |> Some
            | GeoIntersect (field, polygon) ->
                OData.geoIntersect field polygon |> Some
            | OData expr -> Some expr
            | Not filter -> eval filter |> Option.map (sprintf "not (%s)")
            | Binary (filter1, operator, filter2) ->
                match eval filter1, eval filter2 with
                | Some f1, Some f2 -> sprintf "(%s %s %s)" f1 operator.LowerCaseValue f2 |> Some
                | Some f, None | None, Some f -> Some f
                | None, None -> None
        eval filter

[<RequireQualifiedAccess>]
module OrderBy =

    let evaluate (orderBy : OrderBy) =
        match orderBy with
        | ByField (field, direction) ->
            sprintf "%s %s" field direction.LowerCaseValue
        | ByGeoDistance (field, coordinate, direction) ->
            let distanceOData = OData.geoDistance field coordinate
            sprintf "%s %s" distanceOData direction.LowerCaseValue
        | BySearchScore direction ->
            sprintf "search.score() %s" direction.LowerCaseValue
