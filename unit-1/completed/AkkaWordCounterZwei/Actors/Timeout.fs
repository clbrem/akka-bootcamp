namespace AkkaWordCounterZwei.Actors
open System.Runtime.CompilerServices
open Akka.Actor
open System.Timers
open Akka.FSharp


type TimerMessage =
    | Start 
    | Stop
    

            
            
        
    
        

module Timeout =
    let private timer<'T> shouldRepeat (timeout: System.TimeSpan) onComplete=
        let t = new Timer(timeout)        
        t.AutoReset <- shouldRepeat
        t.Elapsed.AddHandler(
            fun _ _ ->                
                onComplete()
            )
        t
    let private kill (t: Timer option) =
        match t with
        | Some timer -> timer.Stop(); timer.Dispose()
        | None -> ()
        
    let start shouldRepeat timeout onComplete =
        fun (mailbox: Actor<TimerMessage>) ->
            let rec loop (maybeTimer: Timer option) =
                actor {
                    match! mailbox.Receive() with                    
                    | Start ->
                        let sender = mailbox.Sender()
                        if maybeTimer.IsSome then
                            kill maybeTimer
                        let newTimer =
                            timer shouldRepeat timeout (
                                fun () ->
                                    if not shouldRepeat then 
                                      mailbox.Self.Tell(Stop)
                                    sender.Tell onComplete
                                )
                        do newTimer.Start()                            
                        return!                        
                            loop (Some newTimer)                            
                    | Stop ->
                        if maybeTimer.IsSome then
                            kill maybeTimer
                        return! loop None
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
    


