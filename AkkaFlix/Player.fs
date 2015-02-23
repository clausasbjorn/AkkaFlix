namespace AkkaFlix

    open Akka.Actor
    open Akka.FSharp

    type Player() = 
        inherit Actor()

        let player = Player.Context.ActorOf(Props(typedefof<Users>, Array.empty))
        let reporting = Player.Context.ActorOf(Props(typedefof<Reporting>, Array.empty))
    
        let notify event =
            player <! event
            reporting <! event

        override x.OnReceive message =
            match message with
            | :? PlayEvent as event -> notify event
            | _ ->  failwith "Unknown message"