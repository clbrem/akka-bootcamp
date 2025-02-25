open Akka.Actor
open Akka.Hosting
open Microsoft.Extensions.Hosting
open Akka.FSharp
open Akka.Configuration
open AkkaWordCounterZwei
open AkkaWordCounterZwei.Actors
let config = Configuration.load()

let system = System.create "my-system" (Configuration.load())

let actor =
    fun (mailbox: Actor<int>) ->
        mailbox.Context.SetReceiveTimeout(System.TimeSpan.FromSeconds(1.0))
        
        let rec loop () =
            actor {                
                match! mailbox.Receive() with                
                | _ ->
                    return! loop ()
            }
        loop ()

[<EntryPoint>]
let main argv =
    task {
        let actorRef = spawn2 system "my-actor" actor 
        let promise = actorRef.Ask<unit>(1)
        actorRef.Tell(2)
        do! promise
        return 0
    }
    |> _.GetAwaiter().GetResult()
    
