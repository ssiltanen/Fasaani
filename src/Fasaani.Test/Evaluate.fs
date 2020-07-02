module Fasaani.Test.Evaluate

open Expecto
open Fasaani

let evaluateNoneValueMsg = "Evaluate should evaluate to Some when passing other than Filter.Empty"
let equalMismatchMsg = "Evaluated filter should match the correct format"

[<Tests>]
let filter =
    testList "Filter" [

        test "Filter.Empty evaluates to None" {
            Expect.isNone (Filter.evaluate Filter.Empty) "Evaluating empty filter should return None"
        }

        test "where evaluates null to valid OData" {
            let filter = Expect.wantSome (where "column" Eq null |> Filter.evaluate) evaluateNoneValueMsg
            Expect.equal filter "column eq null" equalMismatchMsg
        }

        testProperty "where evaluates string value to valid OData" <| fun (operator: ComparisonOperation, value: string) ->
            match isNull value with
            | true -> ()
            | false ->
                let filter = Expect.wantSome (where "column" operator value |> Filter.evaluate) evaluateNoneValueMsg
                let expected = sprintf "column %s '%s'" operator.LowerCaseValue value
                Expect.equal filter expected equalMismatchMsg

        testProperty "where evaluates int value to valid OData" <| fun (operator: ComparisonOperation, value: int) ->
            let filter = Expect.wantSome (where "column" operator value |> Filter.evaluate) evaluateNoneValueMsg
            let expected = sprintf "column %s %O" operator.LowerCaseValue value
            Expect.equal filter expected equalMismatchMsg

        testProperty "where evaluates float value to valid OData" <| fun (operator: ComparisonOperation, value: float) ->
            let filter = Expect.wantSome (where "column" operator value |> Filter.evaluate) evaluateNoneValueMsg
            match value with
            | v when v = infinity ->
                Expect.equal filter (sprintf "column %s INF" operator.LowerCaseValue) equalMismatchMsg
            | v when v = -infinity ->
                Expect.equal filter (sprintf "column %s -INF" operator.LowerCaseValue) equalMismatchMsg
            | v when v = nan ->
                Expect.equal filter (sprintf "column %s NaN" operator.LowerCaseValue) equalMismatchMsg
            | v ->
                Expect.equal filter (sprintf "column %s %O" operator.LowerCaseValue v) equalMismatchMsg

        testProperty "where evaluates decimal value to valid OData" <| fun (operator: ComparisonOperation, value: decimal) ->
            let filter = Expect.wantSome (where "column" operator value |> Filter.evaluate) evaluateNoneValueMsg
            let expected = sprintf "column %s %O" operator.LowerCaseValue value
            Expect.equal filter expected equalMismatchMsg

        testProperty "whereIsIn evaluates values to valid OData" <| fun (values: string list) ->
            match values with
            | [] -> ()
            | values ->
                let filter = Expect.wantSome (whereIsIn "column" values |> Filter.evaluate) evaluateNoneValueMsg
                let expected =
                    values
                    |> Seq.map (fun str -> if isNull str then "null" else str)
                    |> String.concat ", "
                    |> sprintf "search.in(column, '%s')"
                Expect.equal filter expected equalMismatchMsg

        testProperty "whereIsInDelimited evaluates values to valid OData" <| fun (values: string list) ->
            match values with
            | [] -> ()
            | values ->
                let filter = Expect.wantSome (whereIsInDelimited "column" values '|' |> Filter.evaluate) evaluateNoneValueMsg
                let expected =
                    values
                    |> Seq.map (fun str -> if isNull str then "null" else str)
                    |> String.concat "|"
                    |> sprintf "search.in(column, '%s', '|')"
                Expect.equal filter expected equalMismatchMsg

        testProperty "whereDistance evaluates to valid OData" <| fun (coordinate: Coordinate, operator: ComparisonOperation, distance: decimal) ->
            let filter = Expect.wantSome (whereDistance "column" coordinate operator distance |> Filter.evaluate) evaluateNoneValueMsg
            let (Lat lat, Lon lon) = coordinate
            let expected = sprintf "geo.distance(column, geography'POINT(%M %M)') %s %M" lat lon operator.LowerCaseValue distance
            Expect.equal filter expected equalMismatchMsg

        testProperty "whereIntersects evaluates to valid OData" <|
            fun (coord1: Coordinate, coord2: Coordinate, coord3: Coordinate, coord4: Coordinate) ->
            let filter = Expect.wantSome (whereIntersects "column" coord1 coord2 coord3 coord4 |> Filter.evaluate) evaluateNoneValueMsg
            let (Lat lat1, Lon lon1) = coord1
            let (Lat lat2, Lon lon2) = coord2
            let (Lat lat3, Lon lon3) = coord3
            let (Lat lat4, Lon lon4) = coord4
            let expected = sprintf "geo.intersects(column, geography'POLYGON((%M %M, %M %M, %M %M, %M %M))')" lat1 lon1 lat2 lon2 lat3 lon3 lat4 lon4
            Expect.equal filter expected equalMismatchMsg

        test "isNot of Filter.Empty evaluates to None" {
            Expect.isNone (isNot Filter.Empty |> Filter.evaluate) "Evaluating empty filter should return None"
        }

        test "isNot evaluates to valid OData" {
            let filter = Expect.wantSome (where "column" Eq 1 |> isNot |> Filter.evaluate) evaluateNoneValueMsg
            Expect.equal filter "not (column eq 1)" "Combining two empty filters should result to empty filter"
        }

        test "combineWithAnd two Filter.Emptys evaluates to None" {
            let filter = [ Filter.Empty; Filter.Empty ] |> combineWithAnd |> Filter.evaluate
            Expect.isNone filter "Combining two empty filters should result to empty filter"
        }

        test "combineWithOr two Filter.Emptys evaluates to None" {
            let filter = [ Filter.Empty; Filter.Empty ] |> combineWithOr |> Filter.evaluate
            Expect.isNone filter "Combining two empty filters should result to empty filter"
        }

        test "combineWithAnd Filter.Empty to filter does not change filter" {
            let filter = [ Filter.Empty; where "column" Eq 1 ] |> combineWithAnd |> Filter.evaluate
            let filter = Expect.wantSome filter evaluateNoneValueMsg
            Expect.equal filter "column eq 1" "Combining two empty filters should result to empty filter"
        }

        test "combineWithOr Filter.Empty to filter does not change filter" {
            let filter = [ Filter.Empty; where "column" Eq 1 ] |> combineWithOr |> Filter.evaluate
            let filter = Expect.wantSome filter evaluateNoneValueMsg
            Expect.equal filter "column eq 1" "Combining two empty filters should result to empty filter"
        }

        testProperty "combineWithAnd Filter.Empty location in list is interchangeable" <| fun (column: string, operator: ComparisonOperation, value:int) ->
            let filter = where column operator value
            let combination1 = [ filter; Filter.Empty ] |> combineWithAnd |> Filter.evaluate
            let combination2 = [ Filter.Empty; filter ] |> combineWithAnd |> Filter.evaluate
            Expect.equal combination1 combination2 "Evaluated filters should match no matter where empty filter is in combination"

        testProperty "combineWithOr Filter.Empty location in list is interchangeable" <| fun (column: string, operator: ComparisonOperation, value:int) ->
            let filter = where column operator value
            let combination1 = [ filter; Filter.Empty ] |> combineWithOr |> Filter.evaluate
            let combination2 = [ Filter.Empty; filter ] |> combineWithOr |> Filter.evaluate
            Expect.equal combination1 combination2 "Evaluated filters should match no matter where empty filter is in combination"

        testProperty "combineWithAnd combines filters into valid OData" <|
            fun (operator1: ComparisonOperation, value1:int, operator2: ComparisonOperation, value2: string) ->
            let filter =
                [ where "column1" operator1 value1
                  where "column2" operator2 value2 ]
                |> combineWithAnd
                |> Filter.evaluate
                |> fun filter -> Expect.wantSome filter evaluateNoneValueMsg
            let expected =
                match isNull value2 with
                | true -> sprintf "(column1 %s %i and column2 %s null)" operator1.LowerCaseValue value1 operator2.LowerCaseValue
                | false -> sprintf "(column1 %s %i and column2 %s '%s')" operator1.LowerCaseValue value1 operator2.LowerCaseValue value2
            Expect.equal filter expected equalMismatchMsg

        testProperty "combineWithOr combines filters into valid OData" <|
            fun (operator1: ComparisonOperation, value1:int, operator2: ComparisonOperation, value2: string) ->
            let filter =
                [ where "column1" operator1 value1
                  where "column2" operator2 value2 ]
                |> combineWithOr
                |> Filter.evaluate
                |> fun filter -> Expect.wantSome filter evaluateNoneValueMsg
            let expected =
                match isNull value2 with
                | true -> sprintf "(column1 %s %i or column2 %s null)" operator1.LowerCaseValue value1 operator2.LowerCaseValue
                | false -> sprintf "(column1 %s %i or column2 %s '%s')" operator1.LowerCaseValue value1 operator2.LowerCaseValue value2
            Expect.equal filter expected equalMismatchMsg

    ]

[<Tests>]
let orderBy =
    testList "OrderBy" [

        testProperty "byField evaluates to valid OData" <| fun (direction: Direction) ->
            let orderBy = byField "column" direction |> OrderBy.evaluate
            let expected = sprintf "column %s" direction.LowerCaseValue
            Expect.equal orderBy expected equalMismatchMsg

        testProperty "byDistance evaluates to valid OData" <| fun (coordinate: Coordinate, direction: Direction) ->
            let orderBy = byDistance "column" coordinate direction |> OrderBy.evaluate
            let (Lat lat, Lon lon) = coordinate
            let expected = sprintf "geo.distance(column, geography'POINT(%M %M)') %s" lat lon direction.LowerCaseValue
            Expect.equal orderBy expected equalMismatchMsg

        testProperty "byScore evaluates to valid OData" <| fun (direction: Direction) ->
            let orderBy = bySearchScore direction |> OrderBy.evaluate
            let expected = sprintf "search.score() %s" direction.LowerCaseValue
            Expect.equal orderBy expected equalMismatchMsg

    ]