namespace Wrayngler

module TemplateEngine =
    open System.Text.RegularExpressions

    let compile (variables: seq<string * string>) (template: string) : string =
        let makeVariableRegex (name: string) : Regex =
            let pattern = sprintf "{{\s*%s\s*}}" name
            Regex(pattern)

        let foldTemplate (template: string) (name: string, value: string) : string =
            let regex = makeVariableRegex name
            regex.Replace(template, value)

        variables |> Seq.fold foldTemplate template
