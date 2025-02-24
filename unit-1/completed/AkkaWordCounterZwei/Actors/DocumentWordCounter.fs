namespace AkkaWordCounterZwei.Actors
open Akka.FSharp
open Akka.Actor
open Akka.Util
open Akka.Hosting
open Akka.Event
open AkkaWordCounterZwei

module DocumentWordCounter =
    type private WordCountStatus =
        | Receiving of Map<string, int> * Set<IActorRef>
        | Completed of Map<string, int>
    let rec create (documentId: AbsoluteUri)=
        fun (mailbox:Actor<DocumentMessages>) ->
            let logger = mailbox.Context.GetLogger()            
            let rec loop =
                function
                | Completed wordCounts ->
                    actor {                        
                        match! mailbox.Receive() with
                        | FetchCounts _ ->
                            mailbox.Sender().Tell(CountsTabulatedForDocument (documentId, wordCounts))
                            return! loop (Completed wordCounts)
                        | msg when DocumentId.tryGet msg = Some documentId ->                    
                            do logger.Warning("Received message for document {0} but I have already completed processing.", documentId)
                            return! Completed wordCounts |> loop
                        | msg when DocumentId.exists msg->
                            do logger.Warning("Received message for document {0} but I am responsible for document {1}",DocumentId.get msg, documentId)
                            return! Completed wordCounts |> loop
                        | _ -> return! Completed wordCounts |> loop                    
                    }
                | Receiving (wordCounts, subscribers) ->
                   actor {                       
                       match! mailbox.Receive() with
                       | WordsFound (doc, words) when doc = documentId ->
                           do logger.Debug("Found {0} words in document {1}", List.length words, documentId)
                           return!
                               (
                                   words
                                   |> List.fold (fun acc word ->
                                       match Map.tryFind word acc with
                                       | Some count -> Map.add word (count + 1) acc
                                       | None -> Map.add word 1 acc) wordCounts
                                   , subscribers
                               )
                               |> Receiving
                               |> loop
                       | FetchCounts (doc) when doc = documentId  ->
                           let subs = mailbox.Sender() |> Set.add <| subscribers 
                           return! (wordCounts, subs)
                                   |> Receiving
                                   |> loop
                       | EndOfDocumentReached doc when doc = documentId ->
                           do logger.Debug("End of document reached for {0}.", documentId)
                           subscribers |> Set.iter _.Tell(CountsTabulatedForDocument(documentId, wordCounts))                           
                           return! Completed wordCounts |> loop
                       | msg when DocumentId.exists msg ->
                           do logger.Warning("Received message for document {0} but I am responsible for document {1}",DocumentId.get msg, documentId)
                           return! Receiving (wordCounts, subscribers) |> loop
                       | msg ->
                           mailbox.Unhandled(msg)
                           return! Receiving (wordCounts, subscribers) |> loop                       
                   }
                   
            Receiving (Map.empty, Set.empty) |> loop 
                        
