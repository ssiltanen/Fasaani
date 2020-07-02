module Fasaani.Test.Search

open Expecto

[<CLIMutable>]
type AzureSearchConfig =
    { searchName: string
      adminKey: string
      queryKey: string }

let tests (config : AzureSearchConfig) = testList "Filter" [

    test "Placeholder test" {
        Expect.isTrue true ""
    }

]
