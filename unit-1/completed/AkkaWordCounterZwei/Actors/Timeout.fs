namespace AkkaWordCounterZwei.Actors
open Akka
open Akka.Actor
open Akka.Event
open System.Timers
open Akka.FSharp

type TimerMessage =
    | Start
    | Stop
    
    

module Timeout =
    let private timer shouldRepeat (timeout: System.TimeSpan) onComplete  =
        let t = new Timer(timeout)        
        t.AutoReset <- shouldRepeat
        t.Elapsed.AddHandler(onComplete)
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
                        if maybeTimer.IsSome then
                            kill maybeTimer
                        let newTimer =
                            timer shouldRepeat timeout onComplete
                        do newTimer.Start()                            
                        return!                        
                            loop (Some newTimer)                            
                    | Stop ->
                        if maybeTimer.IsSome then
                            kill maybeTimer
                        return! loop None
                }
            loop None
        
        
        
    

