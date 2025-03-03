open System
open Akka.Actor
open Akka.Hosting
open AkkaWordCounterZwei
open AkkaWordCounterZwei.Actors
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open AkkaWordCounterZwei.Config
open Microsoft.Extensions.Options

[<EntryPoint>]
let main _ =
    let thread = 
        task {
          let hostBuilder =
              HostBuilder()
                  .ConfigureAppConfiguration(
                      fun context builder ->
                          builder
                              .AddJsonFile("appsettings.json", true)
                              .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true)
                              .AddEnvironmentVariables()
                          |> ignore
                          )
                  .ConfigureServices(
                      fun context services ->
                          services
                          |> _.AddHttpClient() 
                          |> WordCounterSettings.AddWordCounterSettings 
                          |> _.AddAkka(
                              "MyActorSystem",
                              fun akkaBuilder sp ->
                                  akkaBuilder.ConfigureLoggers(
                                      _.AddLoggerFactory() >> ignore
                                      )
                                  |> ActorConfiguration.addApplicationActors
                                  |> ActorConfiguration.addStartup(
                                      fun _ registry ->
                                          task {
                                              let settings = sp.GetRequiredService<IOptions<WordCounterSettings>>()
                                              let! jobActor = registry.GetAsync<WordCount>()
                                              let absUris =
                                                  settings.Value.DocumentUris
                                                  |> List.ofArray
                                                  |> List.map AbsoluteUri.ofString                                                  
                                              jobActor.Tell(ScanDocuments(absUris))
                                              match! jobActor.Ask<DocumentMessages>(SubscribeToAllCounts) with
                                              | CountsTabulatedForDocuments (docs, counts)->
                                                  counts
                                                  |> Counter.enumerate
                                                  |> List.iter (fun (word, count) -> Console.WriteLine($"Word Count for {word}: {count} "))
                                              | _ -> Console.WriteLine("No counts found")
                                          }                                       
                                      )
                                  |> ignore
                              )
                          |> ignore
                          )
                  
          let host = hostBuilder.Build()
          do! host.RunAsync()
          return 0
        }
    thread.GetAwaiter().GetResult()
        
        
    
        
