namespace AkkaWordCounterZwei.Config
open System.Net.Http
open Akka.Actor
open Akka.Hosting
open Akka.Routing
open Akka.FSharp
open AkkaWordCounterZwei.Actors

type WordCounterManagerActor = private | WordCounterManagerActor
type ParserActor = private | ParserActor
module ActorConfiguration =
    let addWordCounterActor (builder: AkkaConfigurationBuilder) =
        builder.WithActors(
            fun system registry _ ->
                let actor = spawn system "word-counter-manager" WordCounterManager.create
                registry.Register<WordCounterManagerActor>(actor)                
            )
    
    let addParserActor (builder: AkkaConfigurationBuilder) =
        builder.WithActors(
            fun system registry resolver ->
                let factory = resolver.GetService<IHttpClientFactory>()
                let actor =                    
                    Parser.create factory                    
                    |> spawnOpt system "parsers"
                    <| [SpawnOption.Router (RoundRobinPool(5))]
                registry.Register<ParserActor>(actor) 
                )