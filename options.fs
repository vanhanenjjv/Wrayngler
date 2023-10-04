namespace Wrayngler

open Argu

[<RequireQualifiedAccess>]
type CliArgument =
    | Deploy
    | Destroy
    | Compile
    | Stage of string option

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Deploy -> "TODO"
            | Destroy -> "TODO"
            | Compile -> "TODO"
            | Stage _ -> "TODO"

type Options =
    | Deploy of Stage: string option
    | Destroy of Stage: string option
    | Compile of Stage: string option
    | None

module Options =
    let private parser = ArgumentParser.Create<CliArgument>()

    let parse (args: string array) : Options =
        let results = parser.Parse(args)

        let stage = results.GetResult(CliArgument.Stage)

        if results.Contains CliArgument.Compile then
            Compile stage
        else if results.Contains CliArgument.Deploy then
            Deploy stage
        else if results.Contains CliArgument.Destroy then
            Destroy stage
        else
            None

    let describeArguments () = parser.PrintUsage()
