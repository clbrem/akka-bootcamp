namespace AkkaWordCounterZwei


module TextExtractor =
    open HtmlAgilityPack
    open System
    let private collector (input: string) =
        if String.IsNullOrWhiteSpace(input) then [input] else []
    let private justText (node:HtmlNode) =
        node.NodeType = HtmlNodeType.Text && not (node.ParentNode.Name = "script" || node.ParentNode.Name = "style")
    /// <summary>
    /// Extracts raw text from an HTML document.
    /// </summary>
    /// <remarks>
    /// Shouldn't pick up stuff from script / style tags etc
    /// </remarks>
    /// <param name="htmlDoc"> the HTML document to be extracted </param>
    let extractText (htmlDoc: HtmlDocument) =
        let root = htmlDoc.DocumentNode
        root.Descendants()
        |> Seq.filter justText 
        |> Seq.collect ( _.InnerText.Trim() >> collector )
    let extractTokens : string -> string seq =
        _.Split([|' '; '\n'; '\r'; '\t'|], StringSplitOptions.RemoveEmptyEntries)
        >> Seq.map _.Trim()
        
    

