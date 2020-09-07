#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
nuget Fake.DotNet.Testing.Expecto
nuget Farmer
nuget Newtonsoft.Json //"
#load ".fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq

Target.initEnvironment ()

// These are functions to not run them eagerly
let deployConfig () = "azuredeploysettings.json" |> System.IO.File.ReadAllText |> JObject.Parse
let subscriptionIdOrName () = deployConfig () |> fun conf -> string conf.["subscriptionIdOrName"]
let resourceGroup () = deployConfig () |> fun conf -> string conf.["resourceGroup"]
let nugetKeyEnvVariableName = "NUGET_KEY"
let nugetKey = Environment.environVarOrNone nugetKeyEnvVariableName

let projectPath = "src" </> "Fasaani"
let testProjectPath = "src" </> "Fasaani.Test"
let testAssembly =  Path.getFullName ("src" </> "Fasaani.Test" </> "bin" </> "Release" </> "netcoreapp3.1" </> "Fasaani.Test.dll")
let integrationTestSecretsOutput = Path.getFullName ("src" </> "Fasaani.Test" </> "azureSearchConfig.json")

let cleanAll () =
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs

let deployTestInfra () =
    let mySearch = search {
        name "fasaani-test-search"
        sku Search.Free
    }
    let template = arm {
        location Location.WestEurope
        add_resource mySearch
        output "searchName" mySearch.Name
        output "adminKey" mySearch.AdminKey
        output "queryKey" mySearch.QueryKey
    }

    // These are needed if you have multiple subscriptions
    // as az login just takes the first one from returned subscriptions
    // and because Farmer only runs only az login at the background
    let setSub () =
        subscriptionIdOrName () |> Deploy.Az.setSubscription |> Result.map ignore
    let loginResult =
        if Deploy.Az.isLoggedIn ()
        then setSub ()
        else Deploy.Az.login () |> Result.bind setSub

    match loginResult with
    | Ok _ ->
        let rg = resourceGroup ()
        template
        |> Deploy.execute rg Deploy.NoParameters
        |> fun outputs -> // Write connection details to a file for tests to use
            {| SearchName = outputs.["searchName"]
               AdminKey = outputs.["adminKey"]
               QueryKey = outputs.["queryKey"] |}
            |> JObject.FromObject
            |> fun jobj -> File.WriteAllText(integrationTestSecretsOutput, jobj.ToString())
    | Error msg ->
        failwith msg

let build project =
    DotNet.build (fun defaults -> { defaults with Configuration = DotNet.BuildConfiguration.Release }) project

let createNuget project =
    project |> DotNet.restore (fun defaults -> { defaults with NoCache = true })
    project |> DotNet.pack (fun defaults -> { defaults with Configuration = DotNet.BuildConfiguration.Release })

let pushNuget project =
    let apiKey =
        match nugetKey with
        | Some key -> key
        | None -> failwithf "The NuGet API key must be set in a %s environmental variable" nugetKeyEnvVariableName
    let setNugetPushParams (defaults: NuGet.NuGetPushParams) =
        { defaults with
            Source = Some "https://api.nuget.org/v3/index.json"
            ApiKey = Some apiKey }
    let nupkgFolder = project </> "bin" </> "Release"

    nupkgFolder </> (Directory.findFirstMatchingFile "Fasaani.*.nupkg" nupkgFolder)
    |> Path.getFullName
    |> DotNet.nugetPush (fun defaults -> { defaults with PushParams = setNugetPushParams defaults.PushParams })

Target.create "Clean"           (fun _ -> cleanAll())
Target.create "DeployTestInfra" (fun _ -> deployTestInfra ())
Target.create "Build"           (fun _ -> build projectPath)
Target.create "BuildTests"      (fun _ -> build testProjectPath)
Target.create "Test"            (fun _ -> Expecto.run id [ testAssembly ])
Target.create "Pack"            (fun _ -> createNuget projectPath)
Target.create "Publish"         (fun _ -> pushNuget projectPath)

"Clean" ?=> "Build"
"Clean" ?=> "BuildTests"
"BuildTests" ?=> "Test"

"Publish" <== [ "Pack"; "Test" ]

Target.runOrDefault "Build"