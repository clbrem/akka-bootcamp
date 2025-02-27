namespace AkkaWordCounterZwei.Actors
open System.Net.Http
open System
open System.Threading
open System.Threading.Tasks
open Akka.Dispatch
open AkkaWordCounterZwei
open Akka
open Akka.Event
open Akka.Actor
open Akka.FSharp


type Parser =FunActor<DocumentCommands, obj>

module Parser =
    
    open HtmlAgilityPack
    
    let private chunkSize = 20
    let handleDocument (uri: AbsoluteUri, httpClientFactory: IHttpClientFactory, shutdownCts: CancellationTokenSource) =
        task {
            use requestToken = new CancellationTokenSource(delay=TimeSpan.FromSeconds(5.0))
            use linkedToken = CancellationTokenSource.CreateLinkedTokenSource(requestToken.Token, shutdownCts.Token)
            use client = httpClientFactory.CreateClient()
            let! response = client.GetAsync(AbsoluteUri.value uri, linkedToken.Token)
            let! content = response.Content.ReadAsStringAsync(linkedToken.Token)
            let doc = HtmlDocument()
            doc.LoadHtml(content)
            // extract text
            let text = TextExtractor.extractText doc 
            return text |> Seq.collect (TextExtractor.extractTokens) |> Seq.chunkBySize chunkSize |> Seq.map List.ofArray
        }
    let doAThing =
        ActorTaskScheduler.RunTask(
            fun () -> task {return ()} :> Task
            )
            
    let create (httpClientFactory: IHttpClientFactory) =
        fun (mailbox: Actor<DocumentCommands>) ->
            let shutdownCts = new CancellationTokenSource()
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
                        return! loop()
                    | _ -> return! loop()
                }
            loop ()