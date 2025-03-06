namespace AkkaWordCounter2.App.FSharp.Actors
open System.Web
open Akka.Actor
open AkkaWordCounter2.App.FSharp
open Akka.FSharp

type WordCounterManager = | WordCounterManager of FunActor<IWithDocumentId, obj>

module WordCounterManager =
    let create =
        fun (mailbox: Actor<IWithDocumentId>) ->
            let rec loop () =                
                actor {
                    match! mailbox.Receive() with
                    | msg when DocumentId.exists msg ->
                        let docId = DocumentId.get msg
                        
                        let childName = $"word-counter-%s{HttpUtility.UrlEncode(docId.ToString())}"
                        let child = mailbox.Context.Child(childName)
                        if child.IsNobody() then
                            let newChild = spawn mailbox.Context childName (DocumentWordCounter.create docId )                            
                            newChild.Forward msg                            
                        else
                            child.Forward msg 
                        return! loop ()
                    | msg -> mailbox.Unhandled(msg); return! loop() 
                }
            loop()
            
    

