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
    let options = Options.parse args

    let wranglerConfig =
        "wrayngler.toml"
        |> File.readAllText
        |> TemplateEngine.compile [ "stage", "dev" ]

    wranglerConfig |> File.writeAllText "wrangler.toml"

    match options with
    | Compile -> 0
    | options ->
        let wranglerConfig = Toml.ToModel(wranglerConfig)
        let wranglerConfig = wranglerConfig |> Seq.fold (flattenDictionary []) List.empty

        let accountId =
            wranglerConfig |> List.find (fun (key, _) -> key = "account_id") |> snd

        let wrangler = WranglerClient({ AccountId = accountId })

        let queues = wrangler.listQueues ()

        let doesQueueExist (name: string) : bool =
            queues |> Seq.exists (fun queue -> queue.Name = name)

        for entry in wranglerConfig do
            match entry with
            | Queue name ->
                match options with
                | Deploy ->
                    if doesQueueExist name then
                        printfn $"Queue '{name}' already exists."
                    else
                        wrangler.createQueue name
                        printfn $"Created queue '{name}'."
                | Destroy ->
                    if doesQueueExist name then
                        wrangler.deleteQueue name
                        printfn $"Deleted queue '{name}'."
                    else
                        printfn $"Queue '{name}' does not exist."
                | _ -> ()
            | KVNamespace name -> printfn "KVNamespace: %s" name
            | Unknown(_, _) -> ()

        0
