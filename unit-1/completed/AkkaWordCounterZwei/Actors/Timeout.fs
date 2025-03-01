namespace AkkaWordCounterZwei.Actors
open System
open System.Runtime.CompilerServices
open System.Timers
open Akka.FSharp
open Akka.Actor


type private TimerMessage<'T> =
    | Start 
    | Stop
    | Handler of TimerContent<'T>

module Timeout =
    let private timer (onComplete: 'T,shouldRepeat, timeout: TimeSpan) (mailbox: 'T -> unit) =        
        let t = new Timer(timeout)        
        t.AutoReset <- shouldRepeat
        t.Elapsed.AddHandler(
            fun _ _ -> 
                mailbox onComplete
            )
        t
    let private kill (t: Timer option) =
        match t with
        | Some timer -> timer.Stop(); timer.Dispose()
        | None -> ()
    let start<'T>  (sender: IActorRef, onComplete: 'T,shouldRepeat, timeout: TimeSpan) =
        fun (mailbox: Actor<TimerMessage<'T>>) ->
            let rec loop (maybeTimer: Timer option) =
                actor {
                    match! mailbox.Receive() with                    
                    | Start ->                        
                        kill maybeTimer
                        let newTimer =
                            timer (onComplete, shouldRepeat, timeout) 
                                
                        do newTimer.Start()
                        return!                        
                            loop (Some newTimer)                            
                    | Stop -> 
                        kill maybeTimer
                        return! loop None
                    | Handler ->
                        
                        kill maybeTimer
                        
                }
            loop None
        
[<Extension>]
type TimeoutExtension =
    [<Extension>]
    static member TryChild(this: IActorContext, name: string) =
        let child = this.Child(name)
        if child.IsNobody() then None else Some child
    [<Extension>]
    static member OnTimeout(this: IActorContext, timeout: System.TimeSpan, onComplete: 'T, ?name: string) =
        let name = name |> Option.defaultValue "timeout"
        match this.TryChild(name) with
        | Some actor -> actor.Tell(Timeout.Start)
        | None ->
            let newChild =
                spawn
                    this.System
                    name
                    (Timeout.start
                         false
                         timeout
                         onComplete
                    )
            newChild.Tell(Start )        
        
    
    
