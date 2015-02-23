namespace AkkaFlix

    open Akka.Actor
    open Akka.FSharp
    open System.Collections.Generic

    type Users() = 
        inherit Actor()
    
        let context = Users.Context
        let users = new Dictionary<string, ActorRef>();
    
        let rec findOrSpawn user =
            match users.ContainsKey(user) with
            | true -> users.[user]
            | false ->
                users.Add(user, context.ActorOf(Props(typedefof<User>, [| user :> obj |])))
                findOrSpawn user

        let updateUser user asset =
            (findOrSpawn user) <! asset

        override x.OnReceive message =
            match message with
            | :? PlayEvent as event -> 
                updateUser event.User event.Asset
                printfn "Unique users: %d" users.Count 
            | _ ->  failwith "Unknown message"