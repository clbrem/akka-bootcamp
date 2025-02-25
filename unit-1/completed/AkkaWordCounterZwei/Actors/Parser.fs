namespace AkkaWordCounterZwei.Actors
open System.Net.Http
open System
open System.Threading
open System.Threading.Tasks
open Akka.Dispatch
open AkkaWordCounterZwei
open Akka
open Akka.Actor
open Akka.FSharp




module Parser =
    
    open HtmlAgilityPack
    
    let private chunkSize = 20
    let handleDocument (uri: AbsoluteUri, shutdownCts: CancellationTokenSource) =
        task {
            use requestToken = new CancellationTokenSource(delay=TimeSpan.FromSeconds(5.0))
            use linkedToken = CancellationTokenSource.CreateLinkedTokenSource(requestToken.Token, shutdownCts.Token)
            use client = new HttpClient()
            let! response = client.GetAsync(AbsoluteUri.value uri, linkedToken.Token)
            let! content = response.Content.ReadAsStringAsync(linkedToken.Token)
            let doc = HtmlDocument()
            doc.LoadHtml(content)
            // extract text
            let text = TextExtractor.extractText doc 
            return text |> Seq.collect (TextExtractor.extractTokens) |> Seq.chunkBySize chunkSize
        }
    let doAThing =
        ActorTaskScheduler.RunTask(
            fun () -> task {return ()} :> Task
            )
            
    let create (httpClientFactory: IHttpClientFactory) =
        fun (mailbox: Actor<DocumentCommands>) ->
            let rec loop () =                
                actor {
                    match! mailbox.Receive() with
                    | ScanDocument document ->
                        ActorTaskScheduler.RunTask(
                            fun () ->
                                task {return ()} :> Task
                            )                            
                        return! loop()
                    | _ -> return! loop()
                }
            loop()