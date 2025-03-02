namespace AkkaWordCounterZwei.Test
open AkkaWordCounterZwei.Actors
open Akka.Actor
open Akka.FSharp
open Akka.Event
open Xunit
open Akka.TestKit.Xunit2
open Xunit.Abstractions

type private Testing =
    | Ping
    | Pong 
    
type Timeout_Spec(helper: ITestOutputHelper) as this =
    inherit TestKit(config="akka.loglevel=INFO",output=helper)    
    let timerName = "timer"
    let timeout = System.TimeSpan.FromSeconds(0.5)
    let actorA = 
        fun (mailbox: Actor<Testing>) ->            
            let rec loop sender =
                actor {
                    match! mailbox.Receive() with
                    | Ping ->
                        mailbox.Context.OnTimeout(Pong, timerName, timeout)                        
                        return! loop (mailbox.Sender()|> Some)
                    | Pong ->
                        match sender with
                        | Some s -> s.Tell "Done!"
                        | _ -> ()                        
                        return! loop None
                        }
            loop None
    let actorB =
        fun (mailbox: Actor<Testing>) ->
            let logger = mailbox.Context.GetLogger()
            let rec loop (sender: IActorRef option) shouldPong =
                if shouldPong then
                    actor {
                            match! mailbox.Receive() with
                            | Ping ->
                                mailbox.Context.OnTimeout(Pong, timerName, timeout)                        
                                return! loop sender shouldPong 
                            | Pong ->
                                match sender with
                                | Some s -> s.Tell "Done!"
                                | _ -> ()                        
                                return! loop None false
                                }
                else
                    actor {
                            match! mailbox.Receive() with
                            | Ping ->
                                mailbox.Context.OnTimeout(Ping, timerName, timeout)                        
                                return! loop (mailbox.Sender()|> Some) true 
                            | Pong ->
                                
                                match sender with
                                | Some s -> s.Tell "Done!"
                                | _ -> ()                        
                                return! loop None false
                                }
                
            loop None false
    let actorC = 
        fun (mailbox: Actor<Testing>) ->            
            let rec loop sender =
                actor {
                    match! mailbox.Receive() with
                    | Ping ->
                        mailbox.Context.OnTimeout(Pong, timeout=timeout)                        
                        return! loop (mailbox.Sender()|> Some)
                    | Pong ->
                        match sender with
                        | Some s -> s.Tell "Done!"
                        | _ -> ()                        
                        return! loop None
                        }
            loop None            
    [<Fact>]
    let ``Testing Timeout``() =
        task {
            let actor = spawn this.Sys "test" actorA
            let! resp = actor.Ask<string>(Ping)
            Assert.Equal("Done!", resp)
        }
    [<Fact>]
    let ``Testing Timeout 2``() =
        task {
            let actor = spawn this.Sys "test" actorB
            let! resp = actor.Ask<string>(Ping)
            Assert.Equal("Done!", resp)
        }
    [<Fact>]
    let ``Testing alternative timers``() =
        task {
            let actor = spawn this.Sys "test" actorC
            let! resp = actor.Ask<string>(Ping)
            Assert.Equal("Done!", resp)
        }
