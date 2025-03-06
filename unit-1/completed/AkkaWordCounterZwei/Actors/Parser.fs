namespace AkkaWordCounterZwei.Actors
open System
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Akka.Dispatch
open AkkaWordCounterZwei
open Akka.Event
open Akka.Actor
open Akka.FSharp


type Parser = |Parser of  FunActor<DocumentMessages, obj>

module Parser =
    
    open HtmlAgilityPack
    
    let private chunkSize = 20
    let handleDocument (uri: AbsoluteUri, httpClientFactory: IHttpClientFactory, cts: CancellationTokenSource) =
        task {
            use requestToken = new CancellationTokenSource(TimeSpan.FromSeconds(5.0))
            use linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, requestToken.Token)
            use client = httpClientFactory.CreateClient()
            let url = AbsoluteUri.value uri
            let! response = client.GetAsync(url, linkedToken.Token)
            let! content = response.Content.ReadAsStringAsync(linkedToken.Token)
            let doc = HtmlDocument()
            doc.LoadHtml(content)
            let text = TextExtractor.extractText doc
            let tokens = text |> Seq.collect (TextExtractor.extractTokens) |> Seq.chunkBySize chunkSize |> Seq.map List.ofArray            
            return tokens 
        }

    let create (httpClientFactory: IHttpClientFactory) =
        fun (mailbox: Actor<DocumentMessages>) ->            
            let shutdownCts = new CancellationTokenSource();
            let logger = mailbox.Context.GetLogger()
            let rec loop () =                
                actor {
                    match! mailbox.Receive() with
                    | ScanDocument document ->
                        ActorTaskScheduler.RunTask(
                            fun () ->
                                task {
                                    do logger.Debug("Processing Document {0}", document)
                                    let sender = mailbox.Sender()                                    
                                    try                                        
                                        let! features = handleDocument(document, httpClientFactory, shutdownCts)
                                        do features |> Seq.iter (fun f -> sender.Tell(WordsFound(document, f)))
                                        do sender.Tell (EndOfDocumentReached document)                                        
                                    with
                                    | ex ->
                                        do logger.Error(ex, "Error Processing Document {0}", document)
                                        do sender.Tell (DocumentScanFailed (document, ex.Message))
                                } :> Task
                            )                            
                        return! loop ()
                    | _ -> return! loop ()
                }
            loop ()