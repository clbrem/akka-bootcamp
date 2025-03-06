namespace AkkaWordCounter2.App.FSharp
open Akka.Actor
open Akka.FSharp

open Xunit
open Akka.TestKit.Xunit2
open Xunit.Abstractions


type DocumentWordCounter_Spec(helper: ITestOutputHelper) as this=
    inherit TestKit(config="akka.loglevel=DEBUG",output=helper)
    let testDocumentUri = AbsoluteUri.ofString "https://example.com/test"
    [<Fact>]
    let ``Should Process Counts Correctly`` () =
        task {
            let myActor = spawn this.Sys "test" (Actors.DocumentWordCounter.create testDocumentUri)
            let messages = [
                WordsFound(testDocumentUri, ["hello"; "world"])
                WordsFound(testDocumentUri, ["bar";  "foo"])
                WordsFound(testDocumentUri, ["HelLo"; "wOrld"])
                EndOfDocumentReached(testDocumentUri)
            ]
            // have the actor sub to updates
            myActor.Tell(FetchCounts testDocumentUri)
            for msg in messages do
                myActor.Tell(msg)
            let! resp = this.ExpectMsgAsync<DocumentMessages>()
            match resp with
            | CountsTabulatedForDocument (_, m) ->
              Assert.Equal(6, Map.count m)
            | _ -> Assert.True(false)
        }