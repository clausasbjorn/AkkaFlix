(*
    AkkaFlix, a streaming company, needs a scalable backend system.

    ---

    Their backend needs to handle an event called "PlayEvent" that will occur every time one 
    of their users start watching a video asset. The Play-event carries two pieces of 
    information, the "User" (username) and the "Asset" (name of the video).
 
    The PlayEvent is rich in the sense that it can be used for many purposes, the company 
    requires that the system uses the event to perform a couple of business critical tasks:
 
    - Keeps track of how many people are streaming, for statistics.

    - Keeps track of what the individual user is watching, for use by the user interface
      "You are currently watching"-feature.

    - Keeps track of how many times the individual video assets have been streamed, 
      for reporting to content owners.
 
    ---

    We will handle this by creating a hierarchy of actors. At the base a "Player"-actor will 
    receive the event an send it on to two child-actors: "Users" and "Reporting". Users will 
    create a child-actor "User" for each user.
 
    - "User"          Keep track of what the individual user is watching
    - "Users"         Keeps track of how many are watching
    - "Reporting"     Keeps track of how many times assets have been watched
 
    As the events arrive they are tunnelled down through the hierarchy, and the model is 
    kept up-to-date.
 
    ---

    The data is "queried" by outputting the state to the console when it changes. The random 
    arrival of the text in the console illustrates the parallel nature of the actor model.
*)

namespace AkkaFlix

    open System
    open System.Collections.Generic
    open Akka.Actor

    open Akka.FSharp

    type Event =
        | Play of user : string * asset : string

    module AkkaFlix =
    
        // Prepare some test data
        let users = [| "Jack"; "Jill"; "Tom"; "Jane"; "Steven"; "Jackie" |]
        let assets = [| "The Sting"; "The Italian Job"; "Lock, Stock and Two Smoking Barrels"; "Inside Man"; "Ronin" |]

        let rnd = System.Random()

        // Send a Play-event with a randomly selected user and asset to the Player-actor.
        // Continue sending every time a key other than [Esc] is pressed.
        let rec feedPlayerWithEvents player =
            player <! Play(users.[rnd.Next(users.Length)], asset = assets.[rnd.Next(assets.Length)])
            match Console.ReadKey().Key with
            | ConsoleKey.Escape -> ()
            | _ -> feedPlayerWithEvents player

        // Create an actor system and the root Player-actor and pass it to the
        // input loop to start sending Play-events to it.
        [<EntryPoint>]
        let main argv =    

            // Create a user actor for a single user identified by username
            let createUser parent username = 
                spawn parent username
                    (fun mailbox -> 
                        let rec loop(user) = actor {
                            let! message = mailbox.Receive()
                            printfn "%s is watching %s" user message
                            return! loop(user)
                        }                        
                        loop(username))

            // Create users actor (keeps track of all users)
            let createUsers parent = 
                spawn parent "users" 
                    (fun mailbox -> 
                        // Find or add an actor for a user identified by username
                        let rec findOrSpawn (users : Dictionary<string, ActorRef>) username =
                            match users.ContainsKey(username) with
                            | true -> users.[username]
                            | false ->
                                users.Add(username, createUser mailbox username)
                                findOrSpawn users username

                        // Send the asset to the user actor to update it
                        let updateUser users user asset =
                            (findOrSpawn users user) <! asset

                        // Handle incoming messages
                        let rec loop(users) = actor {
                            let! message = mailbox.Receive()
                            match message with
                            | Play(user, asset) ->
                                updateUser users user asset
                            return! loop(users)
                        }
                        loop(new Dictionary<string, ActorRef>()))

            // Create reporting actor
            let createReporting parent =
                spawn parent "reporting" 
                    (fun mailbox -> 
                        // Update the statistics for the asset
                        let registerView (counters : Dictionary<string, int>) asset =
                            match counters.ContainsKey(asset) with
                            | true -> counters.[asset] <- counters.[asset] + 1
                            | false -> counters.Add(asset, 1)

                        // Print the hi score
                        let printReport counters =
                            counters
                            |> Seq.sortBy (fun (KeyValue(k, v)) -> -v) 
                            |> Seq.iter (fun (KeyValue(k, v)) -> printfn "%d\t%s" v k)

                        // Handle incoming messages
                        let rec loop(counters) = actor {
                            let! message = mailbox.Receive()
                            match message with
                            | Play(asset = asset) -> 
                                registerView counters asset
                                printReport counters
                            return! loop(counters)
                        }
                        loop(new Dictionary<string, int>()))

            // Create our Akka.NET actor system
            let system = System.create "akkaflix" (Configuration.load())

            // Our "root" actor that receives play events and pass them
            // to "reporting" and "users"
            let player = 
                spawn system "player" 
                    (fun mailbox -> 
                        let rec loop(users, reporting) = actor {                            
                            let! message = mailbox.Receive()
                            users <! message
                            reporting <! message
                            return! loop(users, reporting)
                        }
                        loop(createUsers mailbox, createReporting mailbox))

            // Start sending some play events
            feedPlayerWithEvents player

            system.Shutdown()
            0
