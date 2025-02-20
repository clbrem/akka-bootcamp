namespace AkkaWordCounter
open Akka.FSharp
open Akka.Actor

type CounterCommand =
    | CountTokens of string list
    | ExpectNoMoreTokens
    | FetchCounts of IActorRef
type DocumentCommand =
    | ProcessDocument of string

    

type Counter = Map<string, int>
    
module Counter =
    let empty = Map.empty
    let add token counter =
        if Map.containsKey token counter then
            Map.add token ( 1 + Map.find token counter )  counter
        else
            Map.add token 1 counter
    let counter =
        fun (mailbox: Actor<CounterCommand>) ->
            let rec loop subscribers doneCounting counts = actor {
                let! message = mailbox.Receive()
                match message with
                | CountTokens tokens ->
                    let newCounts =
                        List.foldBack add tokens counts
                    return! loop subscribers doneCounting newCounts
                | ExpectNoMoreTokens ->
                    
                    
                    return! loop Set.empty true counts
                | FetchCounts requester ->
                    if doneCounting then
                        requester.Tell(counts)
                    else
                        return! loop (Set.add requester subscribers) doneCounting counts
            }
            loop Set.empty false empty 
        
    

