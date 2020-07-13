[<AutoOpen>]
module Fasaani.Evaluate

/// Filter helpers

let where field comparison value =
    Comparison (field, comparison, value)

let whereIsIn field values =
    IsIn (field, values)

let whereIsInDelimited field values delimiter =
    IsInDelimited (field, values, delimiter)

let isNot filter =
    Not filter

let combineWithAnd : Filter seq -> Filter =
    Seq.fold (+) Filter.Empty

let combineWithOr : Filter seq -> Filter =
    Seq.fold (*) Filter.Empty

let whereDistance field coordinate comparison value =
    GeoDistance (field, coordinate, comparison, value)

let whereIntersects field coordinates =
    GeoIntersect(field, coordinates)

/// Order by helpers

let byField field direction =
    ByField (field, direction)

let byDistance field coordinate direction =
    ByGeoDistance (field, coordinate, direction)

let bySearchScore direction =
    BySearchScore direction

[<RequireQualifiedAccess>]
module GeoOData =

    let geoDistance (field: string) (coordinate: Coordinate) =
        let (Lat lat, Lon lon) = coordinate
        sprintf "geo.distance(%s, geography'POINT(%M %M)')" field lon lat

    let geoIntersect (field: string) (polygon: Polygon) =
        // TODO: Make some logic to sort coordinates to counterclockwise order
        // and insert first coordinate at the end
        polygon
        |> Seq.map (fun (Lat lat, Lon lon) -> sprintf "%M %M" lon lat) // Notice that Azure search uses ordering lon lat
        |> String.concat ", "
        |> sprintf "geo.intersects(%s, geography'POLYGON((%s))')" field

[<RequireQualifiedAccess>]
module Filter =

    let evaluate filter =
        let rec eval = function
            | Empty -> None
            | Comparison (field, comparison, value) ->
                match value with
                | null -> sprintf "%s %s null" field comparison.LowerCaseValue
                | :? string as str -> sprintf "%s %s '%s'" field comparison.LowerCaseValue str
                | :? float as num when num = infinity -> sprintf "%s %s INF" field comparison.LowerCaseValue
                | :? float as num when num = -infinity -> sprintf "%s %s -INF" field comparison.LowerCaseValue
                | :? float as num when num = nan -> sprintf "%s %s NaN" field comparison.LowerCaseValue
                | value -> sprintf "%s %s %O" field comparison.LowerCaseValue value
                |> Some
            | IsIn (field, values) ->
                let values = Seq.toList values
                match values with
                | [] -> failwith "No values provided to IsIn operation"
                | _ ->
                    values
                    |> Seq.map (fun str -> if isNull str then "null" else str)
                    |> String.concat ", "
                    |> sprintf "search.in(%s, '%s')" field
                    |> Some
            | IsInDelimited (field, values, delimiter) ->
                let values = Seq.toList values
                match values with
                | [] -> failwith "No values provided to IsIn operation"
                | _ ->
                    values
                    |> Seq.map (fun str -> if isNull str then "null" else str)
                    |> String.concat (string delimiter)
                    |> fun str -> sprintf "search.in(%s, '%s', '%c')" field str delimiter
                    |> Some
            | GeoDistance (field, coordinate, comparison, value) ->
                let distanceOData = GeoOData.geoDistance field coordinate
                sprintf "%s %s %O" distanceOData comparison.LowerCaseValue value
                |> Some
            | GeoIntersect (field, polygon) ->
                GeoOData.geoIntersect field polygon |> Some
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
            let distanceOData = GeoOData.geoDistance field coordinate
            sprintf "%s %s" distanceOData direction.LowerCaseValue
        | BySearchScore direction ->
            sprintf "search.score() %s" direction.LowerCaseValue
