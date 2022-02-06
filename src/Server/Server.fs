module Server

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting

open Shared

// https://devcenter.heroku.com/articles/container-registry-and-runtime#dockerfile-commands-and-runtime
let port =
    System.Environment.GetEnvironmentVariable "PORT"
    |> function null -> "8085" | p -> p

[<EntryPoint>]
let main _ =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseStartup(typeof<Startup.Startup>)
                    .UseWebRoot("public")
                    .UseUrls [| "http://0.0.0.0:" + port |]
                    |> ignore)
        .Build()
        .Run()
    0