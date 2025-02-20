// For more information see https://aka.ms/fsharp-console-apps
open Akka
open System
open Akka.Event
open Akka.FSharp
open Akka.Actor
open System.Threading.Tasks

[<EntryPoint>]
let main args = 
    use system = System.create "my-system" (Configuration.load())
    let sender =
        actorOf2 (
            fun actor message ->
                system.Log.Info $"Received: {message}"
                system.Log.Info $"I'm {actor.Self.Path}"
                system.Log.Info $"Sender: {actor.Context.Sender.Path}"
                )
        |> spawn system "sender-actor" 
    let aref =        
        spawn system "my-actor"
            (fun mailbox ->
                let rec loop() = actor {
                    let! message = mailbox.Receive()                    
                    system.Log.Info($"Received: {message}")                    
                    mailbox.Context.Sender.Tell("got it!")
                    // handle an incoming message
                    return! loop()
                }
                loop()
            )
    system.Log.Info("hello")
    aref.Tell("hello", sender)
    aref.Tell("goodbye", sender)
    system.Terminate().GetAwaiter().GetResult() 
    0


