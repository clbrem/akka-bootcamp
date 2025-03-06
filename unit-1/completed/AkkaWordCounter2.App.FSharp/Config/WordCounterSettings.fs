namespace AkkaWordCounter2.App.FSharp.Config
open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options

[<CLIMutable>]
type WordCounterSettings  = {
    DocumentUris: string array
}

type WordCounterSettingsValidator() =
    interface IValidateOptions<WordCounterSettings> with
        member this.Validate(name, options) =
            let errors = 
                [
                    if options.DocumentUris |> Array.isEmpty then
                        yield "DocumentUris must not be empty"
                    if options.DocumentUris |> Array.exists(fun x -> Uri.IsWellFormedUriString(x, UriKind.Absolute) |> not) then
                        yield "DocumentUris must be valid absolute URIs"
                ]
            match errors with
            | [] -> ValidateOptionsResult.Success
            | _ -> ValidateOptionsResult.Fail(errors)

    module WordCounterSettings =
        let AddWordCounterSettings(services: IServiceCollection) =
            services.AddSingleton<IValidateOptions<WordCounterSettings>, WordCounterSettingsValidator>() |> ignore
            services.AddOptionsWithValidateOnStart<WordCounterSettings>().BindConfiguration("WordCounter") |> ignore
            services
