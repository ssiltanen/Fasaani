#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
nuget Farmer
nuget Newtonsoft.Json //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq

Target.initEnvironment ()

// Functions to not need config file to exist in order to run any fake target
let deployConfig () = "azuredeploysettings.json" |> System.IO.File.ReadAllText |> JObject.Parse
let subscriptionIdOrName () = deployConfig () |> fun conf -> string conf.["subscriptionIdOrName"]
let resourceGroup () = deployConfig () |> fun conf -> string conf.["resourceGroup"]

let projectPath = "src" </> "Fasaani"

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
        |> Seq.iter (fun pair -> printfn "%s: %s" pair.Key pair.Value) // TODO: Instead of printing, write these to a file that tests could use se we avoid copy paste and such
    | Error msg ->
        failwith msg

let createNuget project =
    project |> DotNet.restore (fun defaults -> { defaults with NoCache = true })
    project |> DotNet.pack (fun defaults -> { defaults with Configuration = DotNet.BuildConfiguration.Release })

let pushNuget project =
    let apiKey =
        match Environment.environVarOrNone "NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The NuGet API key must be set in a NUGET_KEY environmental variable"
    let setNugetPushParams (defaults: NuGet.NuGetPushParams) =
        { defaults with
            DisableBuffering = true
            ApiKey = Some apiKey }
    project |> DotNet.nugetPush (fun defaults -> { defaults with PushParams = setNugetPushParams defaults.PushParams })

Target.create "Clean"           (fun _ -> cleanAll())
Target.create "DeployTestInfra" (fun _ -> deployTestInfra ())
Target.create "Build"           (fun _ -> DotNet.build id projectPath)
Target.create "Pack"            (fun _ -> createNuget projectPath)
Target.create "Publish"         (fun _ -> pushNuget projectPath)

"Clean"
  ==> "Build"

"Clean"
  ==> "Pack"
  ==> "Publish"

Target.runOrDefault "Build"