namespace AkkaWordCounterZwei.Test
open Akka.Hosting
open AkkaWordCounterZwei
open AkkaWordCounterZwei.Config.ActorConfiguration
open Xunit
open AkkaWordCounterZwei.Actors
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Xunit.Abstractions

type ParserActorSpec(output: ITestOutputHelper) as this=
    inherit Akka.Hosting.TestKit.TestKit(output = output)
    
    let parserActorUri = AbsoluteUri.ofString "https://getakka.net/"
    [<Fact>]
    let ``Should Parse Words``() =
        task {
            let! parserActor = this.ActorRegistry.GetAsync<Parser>()
            let expectResultsProbe = this.CreateTestProbe()
            parserActor.Tell (ScanDocument parserActorUri, expectResultsProbe)            
            let! _ =
                expectResultsProbe.FishForMessageAsync(
                    (function
                    | EndOfDocumentReached _ -> true
                    | _ -> false)
                    )            
            return ()
        }
    
    override _.ConfigureServices(context:HostBuilderContext,services: IServiceCollection) =
        services.AddHttpClient() |> ignore

    override this.ConfigureAkka(builder, provider) =
        builder.ConfigureLoggers(
            fun configBuilder -> configBuilder.LogLevel <- Akka.Event.LogLevel.DebugLevel 
            )
        |> addParserActor
        |> ignore
    
    