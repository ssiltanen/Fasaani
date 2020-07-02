module Fasaani.Test.Main

open System
open System.IO
open System.Text.Json
open Expecto
open Fasaani.Test

[<EntryPoint>]
let main argv =
    let integrationTestResult =
        let configFile = "azureSearchConfig.json"
        if File.Exists configFile then
            let options = JsonSerializerOptions()
            let content = File.ReadAllText configFile
            let config = JsonSerializer.Deserialize<Search.AzureSearchConfig>(content, options)

            [ Search.tests config ]
            |> testList "Integration Tests"
            |> testSequenced
            |> runTestsWithCLIArgs [ Parallel; Summary ] argv
        else 0

    let unitTestResult = Tests.runTestsInAssemblyWithCLIArgs [ Parallel; Summary ] argv

    Math.Clamp(integrationTestResult + unitTestResult, 0, 1)
