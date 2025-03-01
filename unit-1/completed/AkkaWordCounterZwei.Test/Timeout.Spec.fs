namespace AkkaWordCounterZwei.Test
open AkkaWordCounterZwei.Actors
open Akka.Actor
open Akka.FSharp

open Xunit
open Akka.TestKit.Xunit2
open Xunit.Abstractions

type private Testing =
    | Ping
    | Pong 
    

type Timeout_Spec(helper: ITestOutputHelper) as this =
    inherit TestKit(config="akka.loglevel=DEBUG",output=helper)    
    let timerName = "timer"
    let timeout = System.TimeSpan.FromSeconds(2.0)
    let actorok = 
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
    let bactorok =
        fun (mailbox: Actor<Testing>) ->            
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
    [<Fact>]
    let ``Testing Timeout``() =
        task {
            let actor = spawn this.Sys "test" actorok
            let! resp = actor.Ask<string>(Ping)
            Assert.Equal("Done!", resp)
        }
    [<Fact>]
    let ``Testing double timeout 2``() =
        task {
            let actor = spawn this.Sys "test" bactorok
            let! resp = actor.Ask<string>(Ping)
            Assert.Equal("Done!", resp)
        }        


