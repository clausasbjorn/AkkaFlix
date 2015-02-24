namespace AkkaFlix

    open System
    open Akka.Actor
    open Akka.FSharp

    module AkkaFlix =

        // Prepare some test data
        let users = [| "Jack"; "Jill"; "Tom"; "Jane"; "Steven"; "Jackie" |]
        let assets = [| "The Sting"; "The Italian Job"; "Lock, Stock and Two Smoking Barrels"; "Inside Man"; "Ronin" |]

        let rnd = System.Random()

        // Send a Play-event with a randomly selected user and asset to the Player-actor.
        // Continue sending every time a key other than [Esc] is pressed.
        let rec loop player =
            
            player <! { User = users.[rnd.Next(users.Length)] ; Asset = assets.[rnd.Next(assets.Length)] }    

            match Console.ReadKey().Key with
            | ConsoleKey.Escape -> ()
            | _ -> loop player

        // Create an actor system and the root Player-actor and pass it to the
        // input loop to start sending Play-events to it.
        [<EntryPoint>]
        let main argv =    
            let system = System.create "akkaflix" (Configuration.load())
            let player = system.ActorOf(Props(typedefof<Player>, Array.empty))
    
            loop player

            system.Shutdown()
            0
