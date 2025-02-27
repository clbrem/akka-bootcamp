namespace AkkaWordCounterZwei

module Map =
    let merge: Map<string,int> -> Map<string, int> -> Map<string, int> =
        Map.fold (
            fun acc k v ->
                Map.add k (
                    (Map.tryFind k acc |> Option.defaultValue 0) + v) acc)
    let mergeMany: Map<string, int> seq -> Map<string, int> =
        Seq.fold merge Map.empty

