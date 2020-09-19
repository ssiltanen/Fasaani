[<AutoOpen>]
module Fasaani.Config

let defaultConfig (logger : string -> unit) =
    { Log = Some (QueryDetails.ToString >> logger)
      CancellationToken = None }