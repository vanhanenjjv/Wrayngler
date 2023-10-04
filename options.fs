namespace Wrayngler

open Argu

[<RequireQualifiedAccess>]
type CliArgument =
    | Deploy
    | Destroy
    | Compile

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Deploy -> "TODO"
            | Destroy -> "TODO"
            | Compile -> "TODO"

type Options =
    | Deploy
    | Destroy
    | Compile
    | None

module Options =
    let private parser = ArgumentParser.Create<CliArgument>()

    let parse (args: string array) : Options =
        let results = parser.Parse(args)

        if results.Contains CliArgument.Compile then Compile
        else if results.Contains CliArgument.Deploy then Deploy
        else if results.Contains CliArgument.Destroy then Destroy
        else None
