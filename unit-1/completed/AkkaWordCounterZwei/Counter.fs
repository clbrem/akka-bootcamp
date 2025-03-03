namespace AkkaWordCounterZwei

type Counter<'T when 'T : comparison> = Map<'T, int>

module Counter =
    let merge<'T when 'T: comparison>: Counter<'T> -> Counter<'T> -> Counter<'T> =
        Map.fold (
            fun acc k v ->
                Map.add k (
                    (Map.tryFind k acc |> Option.defaultValue 0) + v) acc)
    let mergeMany<'T when 'T : comparison>: Counter<'T> seq -> Counter<'T> =
        Seq.fold merge Map.empty
    let totals<'T when 'T: comparison> (counter: Counter<'T>) =
        Map.fold (fun acc _ v -> acc + v) 0 counter
    let enumerate<'T when 'T: comparison> (counter: Counter<'T>) =
        Map.fold (fun acc k v -> (k, v) :: acc) [] counter

