namespace AkkaWordCounterZwei.Actors


open Akka.Event
open Akka.Hosting
open AkkaWordCounterZwei
open Akka.FSharp
    


type ProcessingStatus =
    | Processing = 0
    | Completed = 1
    | FailedError= 2
    | FailedTimeout = 3

type WordCountState =
   {
       documentsToProcess: Map<AbsoluteUri,ProcessingStatus> 
   }    with static member empty = { documentsToProcess = Map.empty }

type WordCountJobActorStatus =
    | Receiving
    | Running of WordCountState

/// <summary>
/// Responsible for processing a batch of documents
/// </summary>


module WordCountJobActor =
    let create (registry: IActorRegistry)=
        fun (mailbox: Actor<DocumentCommands>) ->
            let wordCounter = registry.Get<IRequiredActor<WordCounterManager>>()
            let parser = registry.Get<IRequiredActor<Parser>>()
            let logger = mailbox.Context.GetLogger()
            
            let rec loop  =
                function
                | Receiving ->
                    actor {
                        match! mailbox.Receive() with
                        | ScanDocuments toScan ->
                            logger.Info("Received scan request for {0}", toScan |> List.length)
                            return! WordCountState.empty |> Running |> loop
                        | _ ->
                            return! loop Receiving
                    }

                | Running state->
                    actor {}
            loop Receiving

