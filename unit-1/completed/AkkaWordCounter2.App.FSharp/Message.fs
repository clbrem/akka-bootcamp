namespace AkkaWordCounter2.App.FSharp

open System

///<summary>
/// Value type for enforcing absolute URIs 
/// </summary>

    

[<Struct>]
[<CustomEquality>]
[<CustomComparison>]
type AbsoluteUri = private AbsoluteUri of Uri    
    with override this.ToString() =
            match this with AbsoluteUri uri -> uri.ToString()         
         override this.Equals(obj: obj) =
             this.ToString() = obj.ToString()
         override this.GetHashCode() =
                 this.ToString().GetHashCode()
         interface IComparable with
           member this.CompareTo(obj) = String.Compare(obj.ToString(), this.ToString())
    
    
module AbsoluteUri =
    
    let create (uri: Uri) =
        if uri.IsAbsoluteUri |> not
        then ArgumentException "Value must be an absolute URL." |> raise
        uri |> AbsoluteUri
    let ofString (uri: string) =
        match Uri.TryCreate(uri, UriKind.Absolute) with
        | true, uri -> uri |> AbsoluteUri
        | _ -> ArgumentException "Value must be a valid absolute URL." |> raise
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

type DocumentMessages =
    | DocumentScanFailed of AbsoluteUri * string
    | WordsFound of AbsoluteUri * string list
    | EndOfDocumentReached of AbsoluteUri
    | CountsTabulatedForDocument of AbsoluteUri * Map<string, int>
    | CountsTabulatedForDocuments of AbsoluteUri list * Map<string, int>
    | FetchCounts of AbsoluteUri
    | ScanDocument of AbsoluteUri
    | ScanDocuments of AbsoluteUri list
    | SubscribeToAllCounts
    | Timeout
            
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
            | ScanDocument uri -> Some uri
            | ScanDocuments _ -> None
            | Timeout  -> None





