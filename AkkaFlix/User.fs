namespace AkkaFlix

    open Akka.FSharp
   
    type User(user) =
        inherit Actor()

        let user = user
        let mutable watching = null

        override x.OnReceive message =
            match message with
            | :? string as asset -> 
                watching <- asset
                printfn "%s is watching %s" user watching
            | _ ->  failwith "Unknown message"