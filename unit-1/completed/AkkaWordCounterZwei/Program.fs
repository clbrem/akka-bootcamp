open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Event
open Akka.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open AkkaWordCounterZwei.Config

[<EntryPoint>]
let main _ =
    let thread = 
        task {
          let hostBuilder =
              HostBuilder()
                  .ConfigureServices(
                      fun context services ->
                          services.AddHttpClient() |> ignore
                          services.AddAkka(
                              "MyActorSystem",
                              fun akkaBuilder sp ->
                                  akkaBuilder.ConfigureLoggers(
                                      _.AddLoggerFactory() >> ignore
                                      )
                                  |> ActorConfiguration.addWordCounterActor
                                  |> ActorConfiguration.addParserActor
                                  |> ignore
                              ) |> ignore
              )
          let host = hostBuilder.Build()
          do! host.RunAsync()
          return 0
        }
    thread.GetAwaiter().GetResult()
        
        
    
        
