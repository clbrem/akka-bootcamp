namespace AkkaWordCounter2.App.FSharp.Config
open System.Net.Http
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Akka.Actor
open Akka.Hosting
open Akka.Routing
open Akka.FSharp
open AkkaWordCounter2.App.FSharp.Actors


module ActorConfiguration =
    let addWordCounterActor (builder: AkkaConfigurationBuilder) =
        builder.WithActors(
            fun system registry _ ->
                let actor = spawn system "word-counter-manager" WordCounterManager.create  
                registry.Register<WordCounterManager>(actor)                
            )
    
    let addParserActor (builder: AkkaConfigurationBuilder) =
        builder.WithActors(
            fun system registry resolver ->
                let factory = resolver.GetService<IHttpClientFactory>()
                let actor =                    
                    Parser.create factory                    
                    |> spawnOpt system "parsers"
                    <| [SpawnOption.Router (RoundRobinPool(5))]
                registry.Register<Parser>(actor) 
                )
    let addJobActor (builder: AkkaConfigurationBuilder) =
        builder.WithActors(
            fun system registry _ ->
                let actor = spawn system "job" (WordCountJobActor.create registry) 
                registry.Register<WordCount>(actor) 
            )
    let addApplicationActors (builder: AkkaConfigurationBuilder) =
        builder
        |> addWordCounterActor
        |> addParserActor
        |> addJobActor
    let addStartup ( startupAction : ActorSystem -> IActorRegistry -> Task )(builder: AkkaConfigurationBuilder) =        
        builder.AddStartup(StartupTask startupAction)
        

type ActorConfigurationExtensions =
    [<Extension>]
    static member AddApplicationActors(builder: AkkaConfigurationBuilder) =
        ActorConfiguration.addApplicationActors builder