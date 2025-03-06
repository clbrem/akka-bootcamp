// For more information see https://aka.ms/fsharp-console-apps
open System.Threading
open Akka
open System
open Akka.Event
open Akka.FSharp
open Akka.Actor
open System.Threading.Tasks
open AkkaWordCounter

[<EntryPoint>]
let main args =
    let thread = 
        task {
            use system = System.create "my-system" (Configuration.load())
            let counter = spawn system "counter" Counter.counterActor
            let parser = spawn system "parser" (Counter.parserActor counter)
            
            parser.Tell(
                """
                It was the best of times it was the worst of times.
                In fair Verona where we lay our scene.
                Happy families are all alike;
                every unhappy family is unhappy in its own way.
                """ |> ProcessDocument
                )
            let factory = System.Func<IActorRef, obj>(fun ref -> FetchCounts ref :> obj)
            let! promise = counter.Ask<Counter>(factory, TimeSpan.FromSeconds(1L), CancellationToken.None)
            printfn $"%A{promise |> Map.filter (fun k v -> v > 1)}"
//            let! resp = promise            
            return 0
        }
    thread.GetAwaiter().GetResult()
    


