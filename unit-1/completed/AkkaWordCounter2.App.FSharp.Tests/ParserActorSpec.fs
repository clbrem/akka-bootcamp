namespace AkkaWordCounter2.App.FSharp
open Akka.Hosting
open AkkaWordCounter2.App.FSharp.Config.ActorConfiguration
open Xunit
open AkkaWordCounter2.App.FSharp.Actors
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
                    function
                      | WordsFound _ -> true
                      | _ -> false
                    )
            let! _ =
                expectResultsProbe.FishForMessageAsync
                    (
                      function
                      | EndOfDocumentReached _ -> true
                      | _ -> false
                    )
            
            return ()
        }
    
    override _.ConfigureServices(context:HostBuilderContext,services: IServiceCollection) =
        services.AddHttpClient() |> ignore

    override this.ConfigureAkka(builder, provider) =
        builder.ConfigureLoggers(
            fun configBuilder ->
                configBuilder.LogLevel <- Akka.Event.LogLevel.DebugLevel                            
            )
        |> addParserActor
        |> ignore
    
    