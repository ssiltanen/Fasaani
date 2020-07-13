﻿module Fasaani.Test.Main

open System.IO
open System.Text.Json
open Expecto

let integrationTestConfig file =
    (File.ReadAllText file, JsonSerializerOptions())
    |> JsonSerializer.Deserialize<IntegrationTests.AzureSearchConfig>

[<EntryPoint>]
let main argv =
    let configFile = "azureSearchConfig.json"
    let ableToRunIntegrationTests = File.Exists configFile
    [ UnitTests.filter
      UnitTests.orderBy
      if ableToRunIntegrationTests then IntegrationTests.tests (integrationTestConfig configFile) ]
    |> testList "Fasaani"
    |> runTestsWithCLIArgs [ Sequenced; Summary ] argv
