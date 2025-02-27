namespace AkkaWordCounterZwei.Config
open Microsoft.Extensions.Configuration

[<CLIMutable>]
type WordCounterSettings  = {
    DocumentUris: string list
}

//type WordCounterSettingsValidator (options: WordCounterSettings)=
    

//module WordCounterSettings =
    

