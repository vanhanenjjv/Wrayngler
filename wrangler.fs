namespace Wrayngler

module Process =
    open System.Diagnostics

    let exec (environmentVariables: seq<string * string>) (fileName: string) (arguments: string seq) : string =
        let addEnvironmentVariable (startInfo: ProcessStartInfo) (name, value) : unit =
            startInfo.EnvironmentVariables.Add(name, value)

        let startInfo = ProcessStartInfo(fileName, arguments |> String.concat " ")
        startInfo.RedirectStandardOutput <- true
        environmentVariables |> Seq.iter (addEnvironmentVariable startInfo)

        let ``process`` = Process.Start(startInfo)
        ``process``.WaitForExit()
        let output = ``process``.StandardOutput.ReadToEnd()

        output

open System.Text.Json
open System.Text.Json.Serialization

type Queue =
    { [<JsonPropertyName "queue_id">]
      Id: string
      [<JsonPropertyName "queue_name">]
      Name: string }

type KVNamespace =
    { [<JsonPropertyName "id">]
      Id: string
      [<JsonPropertyName "title">]
      Title: string
      [<JsonPropertyName "supports_url_encoding">]
      SupportsUrlEncoding: bool }

type WranglerClientOptions = { AccountId: string }

type WranglerClient(options: WranglerClientOptions) =
    let environmentVariables = [ "CLOUDFLARE_ACCOUNT_ID", options.AccountId ]

    let execWrangler (arguments: string list) =
        Process.exec environmentVariables "npx" ("wrangler" :: arguments)

    member _.listQueues() : Queue seq =
        let output = execWrangler [ "queues"; "list" ]
        let queues = JsonSerializer.Deserialize<Queue seq>(output)
        queues

    member _.createQueue(name: string) : unit =
        execWrangler [ "queues"; "create"; name ] |> ignore

    member _.deleteQueue(name: string) : unit =
        execWrangler [ "queues"; "delete"; name ] |> ignore

    member _.listKVNamespaces() : KVNamespace seq =
        let output = execWrangler [ "kv:namespace"; "list" ]
        let kvNamespaces = JsonSerializer.Deserialize<KVNamespace seq>(output)
        kvNamespaces
