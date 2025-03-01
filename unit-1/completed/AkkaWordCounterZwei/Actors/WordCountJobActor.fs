namespace AkkaWordCounterZwei.Actors
open Akka.Event
open Akka.Hosting
open AkkaWordCounterZwei
open Akka.FSharp
open Akka.Actor

type ProcessingStatus =
    | Processing = 0
    | Completed = 1
    | FailedError= 2
    | FailedTimeout = 3

type WordCountState =
   {
       wordCount: Map<AbsoluteUri, Counter<string>>
       documentsToProcess: Map<AbsoluteUri,ProcessingStatus> 
   }    with static member empty = { documentsToProcess = Map.empty; wordCount = Map.empty }
module WordCountState =
    let wordCount (state: WordCountState) (doc: AbsoluteUri) (wordCount:Map<string, int>) =
        
        { state with wordCount = Map.add doc wordCount state.wordCount }
    let setStatus status (state: WordCountState) (doc: AbsoluteUri) =
        { state with documentsToProcess = Map.add doc status state.documentsToProcess }
    let processDocument =
        setStatus ProcessingStatus.Processing
    let completeDocument =
        setStatus ProcessingStatus.Completed
    let failDocument =
        setStatus ProcessingStatus.FailedError
    let timeoutDocument = 
        setStatus ProcessingStatus.FailedTimeout

type WordCountJobActorStatus =
    | Receiving
    | Running of WordCountState
    | JobComplete of bool*WordCountState

/// <summary>
/// Responsible for processing a batch of documents
/// </summary>


module WordCountJobActor =
    let create (registry: IActorRegistry)=
        fun (mailbox: Actor<DocumentMessages>) ->
            let wordCounter = registry.Get<IRequiredActor<WordCounterManager>>()
            let parser = registry.Get<IRequiredActor<Parser>>()
            let logger = mailbox.Context.GetLogger()            
            let rec loop =
                function
                | Receiving ->
                    actor {
                        match! mailbox.Receive() with
                        | ScanDocuments toScan ->
                            logger.Info("Received scan request for {0}", toScan |> List.length)
                            toScan
                            |> List.iter (fun doc ->
                                parser.Tell(ScanDocument doc)
                                wordCounter.Tell (FetchCounts doc)                                
                                )
                            mailbox.UnstashAll()
                            return! toScan
                                    |> List.fold WordCountState.processDocument WordCountState.empty
                                    |> Running
                                    |> loop 
                        | _ ->
                            mailbox.Stash()
                            return! loop Receiving
                    }
                | Running state ->
                    actor {
                        match! mailbox.Receive() with
                        | WordsFound (doc, found) ->
                            wordCounter.Forward(WordsFound (doc,found))
                            return! Running state |> loop  
                        | EndOfDocumentReached doc ->
                            parser.Forward(EndOfDocumentReached doc)
                        | CountsTabulatedForDocument (doc, counts) ->
                            logger.Info("Counts tabulated for {0}", doc)                            
                            return! JobComplete(
                                false,
                                counts |> WordCountState.wordCount state doc
                                |> WordCountState.completeDocument
                                <| doc
                                )|> loop
                        | DocumentScanFailed (doc, reason) ->
                            logger.Error ("Document scan failed for {0}: {1}", doc, reason)
                            return!
                               JobComplete (
                                    false,
                                    WordCountState.failDocument state doc
                                )|> loop                        
                        | _ -> failwith "todo"
                    }
                | JobComplete (force, wordState) ->
                    actor {}
                    
            loop Receiving

