namespace AkkaWordCounterZwei.Actors
open System
open System.Runtime.CompilerServices
open System.Timers
open Akka.FSharp
open Akka.Actor


type private TimerContent =   
    {
     onComplete: obj
     shouldRepeat: bool
     timeout: TimeSpan
    } with
    static member Create(onComplete: obj, timeout: TimeSpan, shouldRepeat: bool) =
        { onComplete = onComplete
          shouldRepeat = shouldRepeat 
          timeout = timeout 
        }

type private TimerMessage =
    | StartTimer 
    | StopTimer
    | Handler of TimerContent

module  private Timeout =    
    let DEFAULT_TIMEOUT = TimeSpan.FromSeconds 3.0
    let timer (callback: obj -> unit)(content: TimerContent)  =        
        let t = new Timer(content.timeout)        
        t.AutoReset <- content.shouldRepeat
        t.Elapsed.AddHandler(
            fun _ _ -> 
                callback content.onComplete
            )
        t
    let  kill (t: Timer option) =
        match t with
        | Some timer -> timer.Stop(); timer.Dispose()
        | None -> ()
    let start(callback: IActorRef ) (content: TimerContent)=
        fun (mailbox: Actor<TimerMessage>) ->
            let rec loop (maybeTimer: Timer option) (content: TimerContent) =
                actor {
                    match! mailbox.Receive() with                    
                    | StartTimer ->                        
                        kill maybeTimer
                        let newTimer =
                            timer callback.Tell content
                        do newTimer.Start()
                        return! loop (Some newTimer) content
                    | StopTimer -> 
                        kill maybeTimer
                        return! loop None content
                    | Handler content -> 
                        return! loop maybeTimer content                          
                }
            loop None content
        
type TimeoutExtension =
    [<Extension>]
    static member TryChild(this: IActorContext, name: string) =
        let child = this.Child(name)
        if child.IsNobody() then None else Some child
    [<Extension>]
    static member OnTimeout(this: IActorContext, onComplete: obj, ?name: string, ?timeout: TimeSpan) =
        let name = name |> Option.defaultValue "timeout"
        let actualTimeout = timeout |> Option.defaultValue Timeout.DEFAULT_TIMEOUT
        let content =  TimerContent.Create(onComplete, actualTimeout, false)        
        match this.TryChild(name) with
        | Some actor ->
            actor.Tell (Handler content)
            actor.Tell StartTimer
        | None ->
            let newChild =
                spawn
                    this
                    name
                    (Timeout.start this.Self content)
            newChild.Tell StartTimer
        
    
    
