open Wrayngler
open Tomlyn

module File =
    open System.IO
    let readAllText (path: string) : string = File.ReadAllText(path)
    let writeAllText (path: string) (contents: string) : unit = File.WriteAllText(path, contents)

open System.Text.RegularExpressions

let makeResourcePredicate (pattern: string) (key: string) =
    let regex = Regex(pattern, RegexOptions.Compiled)
    regex.IsMatch(key)

let isQueue = makeResourcePredicate @"queues\.consumers\.\[\d+\]\.queue"
let isKVNamespace = makeResourcePredicate @"kv_namespaces\.\[\d+\]\.id"

let (|Queue|KVNamespace|Unknown|) (key: string, value: string) =
    if isQueue key then Queue value
    elif isKVNamespace key then KVNamespace value
    else Unknown(key, value)

open System.Collections.Generic

let rec flattenDictionary
    (path: string list)
    (config: list<string * string>)
    (pair: KeyValuePair<string, obj>)
    : list<string * string> =
    match pair.Value with
    | :? IDictionary<string, obj> as value -> value |> Seq.fold (flattenDictionary (pair.Key :: path)) config
    | :? IEnumerable<obj> as value ->
        value
        |> Seq.mapi (fun index value -> KeyValuePair($"[{index}]", value))
        |> Seq.fold (flattenDictionary (pair.Key :: path)) config
    | value ->
        let key = pair.Key :: path |> List.rev |> String.concat "."
        let value = string value
        (key, value) :: config

[<EntryPoint>]
let main (args: string array) : int =
    let compileTemplate (variables: option<string * string> list) =
        let variables = variables |> List.choose id
        "wrayngler.toml" |> File.readAllText |> TemplateEngine.compile variables

    let writeWranglerConfigurationFile (config: string) : string =
        File.writeAllText "wrangler.toml" config
        config

    let parseConfiguration (config: string) : list<string * string> =
        Toml.ToModel(config) |> Seq.fold (flattenDictionary []) List.empty

    let options = Options.parse args

    match options with
    | Compile stage ->
        [ stage |> Option.map (fun stage -> "stage", stage) ]
        |> compileTemplate
        |> writeWranglerConfigurationFile

    | Deploy stage ->
        [ stage |> Option.map (fun stage -> "stage", stage) ]
        |> compileTemplate
        |> writeWranglerConfigurationFile
        |> parseConfiguration
        |> List.iter (function
            | Queue name ->
                if doesQueueExist name then
                    printfn $"Queue '{name}' already exists."
                else
                    wrangler.createQueue name
                    printfn $"Created queue '{name}'."
            | _ -> ())
    | Destroy stage ->
        [ stage |> Option.map (fun stage -> "stage", stage) ]
        |> compileTemplate
        |> writeWranglerConfigurationFile
        |> parseConfiguration
        |> List.iter (function
            | Queue name ->
                if doesQueueExist name then
                    wrangler.deleteQueue name
                    printfn $"Deleted queue '{name}'."
                else
                    printfn $"Queue '{name}' does not exist."
            | _ -> ())
    | None -> printfn (Options.describeArguments ())


    0
