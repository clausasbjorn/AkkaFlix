namespace AkkaFlix

    open System.Collections.Generic
    open Akka.FSharp

    type Reporting() =
        inherit Actor()
    
        let counters = new Dictionary<string, int>();

        let registerView asset =
            match counters.ContainsKey(asset) with
            | true -> counters.[asset] <- counters.[asset] + 1
            | false -> counters.Add(asset, 1)

        let printReport h =
            h
            |> Seq.sortBy (fun (KeyValue(k, v)) -> -v) 
            |> Seq.iter (fun (KeyValue(k, v)) -> printfn "%d\t%s" v k)
        
        override x.OnReceive message =
            match message with
            | :? PlayEvent as event -> 
                registerView event.Asset
                printReport counters
            | _ ->  failwith "Unknown message"