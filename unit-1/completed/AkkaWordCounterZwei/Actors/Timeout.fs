namespace AkkaWordCounterZwei.Actors
open System
open System.Runtime.CompilerServices
open Akka.Actor
open System.Timers
open Akka.FSharp


type private TimerContent<'T>(onComplete: 'T, ?timeout: TimeSpan, ?shouldRepeat: bool ) =
    member _.Timer(mailbox: 'T -> unit) =
        let actualTimeout = timeout |> Option.defaultValue (TimeSpan.FromSeconds 3.0)
        let actualRepeat = shouldRepeat |> Option.defaultValue false
        let t = new Timer(actualTimeout)        
        t.AutoReset <- actualRepeat
        t.Elapsed.AddHandler(
            fun _ _ -> 
                mailbox onComplete
            )
        t    

type private TimerMessage<'T> =
    | Start 
    | Stop
    | Handler of TimerContent<'T>

module Timeout<'T> =
    let private timer (shouldRepeat, timeout: System.TimeSpan, onComplete)=
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
                        kill maybeTimer
                        let newTimer =
                            timer (shouldRepeat, timeout, 
                                fun () ->
                                    if not shouldRepeat then 
                                      mailbox.Self.Tell(Stop)
                                    sender.Tell onComplete
                                )
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
        
    
    
