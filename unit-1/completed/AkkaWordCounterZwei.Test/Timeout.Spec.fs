namespace AkkaWordCounterZwei.Test
open AkkaWordCounterZwei.Actors
open Akka.Actor
open Akka.FSharp

open AkkaWordCounterZwei.Actors
open Xunit
open Akka.TestKit.Xunit2
open Xunit.Abstractions

type private Testing =
    | Ping
    | Pong 
    

type Timeout_Spec(helper: ITestOutputHelper) as this =
    inherit TestKit(config="akka.loglevel=DEBUG",output=helper)    
    let timerName = "timer"
    let timeout = System.TimeSpan.FromSeconds(5.0)
    let actorok = 
        fun (mailbox: Actor<Testing>) ->            
            let rec loop sender =
                actor {
                    match! mailbox.Receive() with
                    | Ping ->
                        mailbox.Context.OnTimeout(timeout, Pong, timerName)                        
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
            let actor = spawn this.Sys "test" actorok
            let! resp = actor.Ask<string>(Ping)
            Assert.Equal("Done!", resp)
        }


