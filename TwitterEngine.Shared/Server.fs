﻿namespace TwitterEngine.Shared

module Server =
    open System
    open System.IO
    open System.Reflection
    open Akkling
    open WebSharper.AspNetCore.WebSocket.Server
    open TwitterEngine.Shared.Types
    open Serilog

    let setupLogging () =
        let logDirectory = Path.Combine(Path.GetTempPath(), Assembly.GetCallingAssembly().FullName)
        if not <| System.IO.Directory.Exists(logDirectory)
            then Directory.CreateDirectory(logDirectory) |> ignore

        let sessionId = DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString()
        let logFile = sprintf "logs-%s.json" sessionId
        let filePath = Path.Combine(logDirectory, logFile)
        File.Create(filePath).Dispose()

        let log = (((LoggerConfiguration()).MinimumLevel.Debug()).WriteTo).File(filePath).CreateLogger()
        Serilog.Log.Logger <- log

        sessionId

    let Start : Agent<S2CMessage, C2SMessage> =
        /// print to debug output and stdout
        let dprintfn x =
            Printf.ksprintf (fun s ->
                System.Diagnostics.Debug.WriteLine s
                stdout.WriteLine s
            ) x

        use system = System.create "server" <| Configuration.load()
        let supervisorActorRef = spawn system "supervisor" <| props(TwitterEngine.Shared.Actors.superviserActor)
        let sessionId = setupLogging ()

        fun client -> async {
            let clientIp = client.Connection.Context.Connection.RemoteIpAddress.ToString()
            return fun msg ->
                dprintfn "Received message #%A from %s" msg clientIp
                match msg with
                | Message data ->
                    let supervisorRequest =
                        match data with 
                        | Signup creds -> 
                            SuperviserRequest.Signup (creds, client.PostAsync >> ignore)
                        | UserRequest req -> 
                            SuperviserRequest.UserRequest(req)
                    supervisorActorRef <! supervisorRequest
                | Message.Error a -> 
                    dprintfn "Exception occurred: %O" a
                | Close -> 
                    dprintfn "Closing connection"
            }    