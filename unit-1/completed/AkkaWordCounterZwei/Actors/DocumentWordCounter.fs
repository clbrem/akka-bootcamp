namespace AkkaWordCounterZwei.Actors
open Akka.FSharp
open Akka.Actor
open Akka.Util
open Akka.Hosting
open Akka.Event
open AkkaWordCounterZwei

module DocumentWordCounter =
    let rec create (documentId: AbsoluteUri)=
        fun (mailbox:Actor<DocumentQueries>) ->
            let rec complete wordCounts =
                actor {
                    match! mailbox.Receive() with
                    | FetchCounts _ ->
                        mailbox.Sender().Tell(CountsTabulatedForDocument (documentId, wordCounts))
                        return! complete wordCounts
                    | msg when DocumentId.tryGet msg = Some documentId ->                    
                        do mailbox.Log.Value.Warning("Received message for document {0} but I have already completed processing.", documentId)
                        return! complete wordCounts
                    | msg when DocumentId.exists msg->
                        mailbox.Log.Value.Warning("Received message for document {0} but I am responsible for document {1}",DocumentId.get msg, documentId)
                        return! complete wordCounts
                    | _ -> return! complete wordCounts
                }
            complete Map.emptyg
                        
