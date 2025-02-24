namespace AkkaWordCounterZwei

open System

///<summary>
/// Value type for enforcing absolute URIs 
/// </summary>

[<Struct>]
type AbsoluteUri = private AbsoluteUri of Uri 
    with override this.ToString() =
            match this with AbsoluteUri uri -> uri.ToString()
    
module AbsoluteUri =
    
    let create (uri: Uri) =
        if uri.IsAbsoluteUri |> not
        then ArgumentException "Value must be an absolute URL." |> raise
        AbsoluteUri uri
        
    let value (absoluteUri: AbsoluteUri) =
        match absoluteUri with
        | AbsoluteUri uri -> uri
        
type IWithDocumentId =
    abstract member DocumentId: AbsoluteUri option

module DocumentId =
    let get (doc: #IWithDocumentId) =
        (doc:>IWithDocumentId).DocumentId.Value
    let tryGet (doc: #IWithDocumentId) =
        (doc:>IWithDocumentId).DocumentId
    let exists (doc: #IWithDocumentId) =
        (doc:>IWithDocumentId).DocumentId
        |> Option.isSome

type DocumentCommands =
    | ScanDocument of AbsoluteUri
    | ScanDocuments of AbsoluteUri list
    interface IWithDocumentId with
        member this.DocumentId = 
            match this with
            | ScanDocument uri -> Some uri
            | ScanDocuments _ -> None

type DocumentMessages =
    | DocumentScanFailed of AbsoluteUri * string
    | WordsFound of AbsoluteUri * string list
    | EndOfDocumentReached of AbsoluteUri
    | CountsTabulatedForDocument of AbsoluteUri * Map<string, int>
    | CountsTabulatedForDocuments of AbsoluteUri list * Map<string, int>
    | FetchCounts of AbsoluteUri
    | SubscribeToAllCounts    
            
    interface IWithDocumentId with
        member this.DocumentId = 
            match this with
            | DocumentScanFailed (uri, _) -> Some uri
            | WordsFound (uri, _) -> Some uri
            | EndOfDocumentReached uri -> Some uri
            | CountsTabulatedForDocument (uri, _) -> Some uri
            | FetchCounts uri -> Some uri
            | SubscribeToAllCounts -> None
            | CountsTabulatedForDocuments (uris, _) -> None





