open Akka.Actor
open Akka.FSharp
open Akka.Event

let config = Configuration.load()
let system = System.create "my-system" (Configuration.load())

type Heartbeat = 
    | Pulse
    | Start
    | Stop

let timer callback =
    let t = new System.Timers.Timer(100.0, AutoReset = true)
    t.Elapsed.Add(callback)
    t.Start()
    t

let tryStop (t: System.Timers.Timer option) =
    match t with
    | Some t -> t.Stop()
    | None -> ()

let heartbeatRecipient =
    fun (mailbox: Actor<Heartbeat>) ->
        let rec loop =
            function
            | Some (tee: System.Timers.Timer) ->
                actor {
                    match! mailbox.Receive() with
                    | Pulse ->
                        mailbox.Log.Value.Info("BA-DUM")
                        return! loop (Some tee)
                    | Start ->
                        return! loop (Some tee)
                    | Stop ->
                        mailbox.Log.Value.Info("Stopping...")
                        tee.Stop()
                        return! loop None            
                }
            | None ->
                actor {
                    match! mailbox.Receive() with                    
                    | Start ->
                        mailbox.Log.Value.Info("Starting...")
                        let t = timer (fun _ -> mailbox.Self.Tell(Pulse))
                        return! loop (Some t)
                    | _ ->
                        return! loop None
                }
        loop None



[<EntryPoint>]
let main _ =
    task {
        let actorRef = spawn system "my-actor" heartbeatRecipient 
        actorRef.Tell(Start)        
        do! System.Threading.Tasks.Task.Delay(5000)
        actorRef.Tell(Stop)
        do! System.Threading.Tasks.Task.Delay(1000)
        return 0
    }
    |> _.GetAwaiter().GetResult()
    
