namespace AkkaWordCounter
open Akka.Event
open Akka.FSharp
open Akka.Actor
open System

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
    let counterActor =
        fun (mailbox: Actor<CounterCommand>) ->
            let rec loop subscribers doneCounting counts =
                actor {
                    let! message = mailbox.Receive()
                    match message with
                    | CountTokens tokens ->
                        let newCounts =
                            List.foldBack add tokens counts                        
                        return! loop subscribers doneCounting newCounts
                    | ExpectNoMoreTokens ->
                        mailbox.Log.Value.Info(
                            "Completed counting tokens - found [{0}] unique tokens",
                            Map.count counts
                            )
                        for subscriber: IActorRef in subscribers do
                            subscriber.Tell(counts)
                        return! loop Set.empty true counts
                    | FetchCounts ref when doneCounting ->                            
                            ref.Tell(counts)
                            return! loop subscribers doneCounting counts
                    | FetchCounts ref ->
                            return! loop (Set.add ref subscribers) doneCounting counts
                }
            loop Set.empty false empty
    let private TOKEN_BATCH_SIZE = 10;
    let parserActor (counter: IActorRef)=
        fun (mailbox: Actor<DocumentCommand>) ->
            let rec loop () = 
                actor {
                    match! mailbox.Receive() with
                    | ProcessDocument str ->                        
                        str.Split(" ")
                        |> List.ofArray
                        |> List.filter (String.IsNullOrWhiteSpace >> not) 
                        |> List.chunkBySize TOKEN_BATCH_SIZE
                        |> List.iter (CountTokens >> counter.Tell)
                        counter.Tell(ExpectNoMoreTokens)
                    return! loop ()
                }
            loop ()
            
            