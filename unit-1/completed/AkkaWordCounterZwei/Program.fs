open Akka.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open AkkaWordCounterZwei.Config

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
        
        
    
        
