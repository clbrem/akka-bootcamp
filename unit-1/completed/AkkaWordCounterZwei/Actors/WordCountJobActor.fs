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
       subscribers: Set<IActorRef>
   }    with static member empty =
               {
                   documentsToProcess = Map.empty
                   wordCount = Map.empty
                   subscribers = Set.empty
               }
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
    let isCompleted =
        _.documentsToProcess
        >> Map.exists (fun _ status -> status = ProcessingStatus.Processing)
        >> not
    let enumerateStatus state = 
        state.documentsToProcess
        |> Map.toList
        |> List.map (fun (uri, status) -> (uri, status, Map.find uri state.wordCount |> Counter.totals )) 
    let broadcast (state: WordCountState) (msg: DocumentMessages) =
        state.subscribers
        |> Set.iter (fun sub -> sub.Tell msg)

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
                    if force || (WordCountState.isCompleted wordState) then 
                        actor {
                             do WordCountState.enumerateStatus wordState
                                |> List.iter (
                                    fun (doc, status, count) ->
                                        logger.Info("Document {0} status: {1}, total words: {2}", doc, status, count)
                                    )
                             let mergedCounts = wordState.wordCount |> Map.values |> Counter.mergeMany
                             do CountsTabulatedForDocuments(Map.keys wordState.documentsToProcess |> List.ofSeq, mergedCounts)
                                |> WordCountState.broadcast wordState
                             do mailbox.Context.Stop(mailbox.Self)
                             return ()
                        }
                    else
                        actor {
                            return! loop (Running wordState)
                        }
            loop Receiving

