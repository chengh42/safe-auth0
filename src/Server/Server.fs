module Server

open System
open System.Security.Claims
open System.Threading.Tasks
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
open Saturn
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting

open Shared

type Storage() =
    let todos = ResizeArray<_>()

    member __.GetTodos() = List.ofSeq todos

    member __.AddTodo(todo: Todo) =
        if Todo.isValid todo.Description then
            todos.Add todo
            Ok()
        else
            Error "Invalid todo"

let storage = Storage()

storage.AddTodo(Todo.create "Create new SAFE project")
|> ignore

storage.AddTodo(Todo.create "Write your app")
|> ignore

storage.AddTodo(Todo.create "Ship it !!!")
|> ignore

let todosApi =
    { getTodos = fun () -> async { return storage.GetTodos() }
      addTodo =
          fun todo ->
              async {
                  match storage.AddTodo todo with
                  | Ok () -> return todo
                  | Error e -> return failwith e
              } }

let securedApi (ctx: HttpContext) =
    for pair in ctx.Request.Headers do
        printfn $"key = { pair.Key.ToLower() }, value = { pair.Value.[0] }"

    { getMessage = fun () -> async { return "Hello from a private endpoint! You need to be authenticated to see this." } }

let publicApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue todosApi
    |> Remoting.buildHttpHandler

let securedApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext securedApi
    |> Remoting.buildHttpHandler

let webApp =
    choose [
        publicApp
        securedApp
    ]

// https://devcenter.heroku.com/articles/container-registry-and-runtime#dockerfile-commands-and-runtime
let port =
    System.Environment.GetEnvironmentVariable "PORT"
    |> function null -> "8085" | p -> p

type HasScopeRequirement (scope: string, issuer: string) =
    interface IAuthorizationRequirement
    member _.Scope = if isNull scope then raise (ArgumentNullException(nameof scope)) else scope
    member _.Issuer = if isNull issuer then raise (ArgumentNullException(nameof issuer)) else issuer

type HasScopeHandler =
    inherit AuthorizationHandler<HasScopeRequirement>
    override this.HandleRequirementAsync (ctx, requirement) =
        // If user does not have the scope claim, get out of here
        if ctx.User.HasClaim (fun c -> c.Type = "scope" && c.Issuer = requirement.Issuer)
            then Task.CompletedTask
            else
                // Split the scopes string into an array
                let scopes = ctx.User
                                .FindFirst(fun c -> c.Type = "scope" && c.Issuer = requirement.Issuer)
                                .Value.Split(" ")

                // Succeed if the scope array contains the required scope
                if scopes |> Array.exists (fun s -> s = requirement.Scope)
                    then ctx.Succeed requirement

                Task.CompletedTask

let configureApp (app:IApplicationBuilder) =
    app
        .UseAuthentication()
        .UseAuthorization()

let configureServices (services : IServiceCollection) =
    let config = services.BuildServiceProvider().GetService<IConfiguration>()
    let domain = config.["Auth0:Domain"]
    let audience = config.["Auth0:Audience"]

    services
        .AddAuthorization(fun options ->
            options.AddPolicy("read:messages", (fun policy ->
                policy.AddRequirements(new HasScopeRequirement("read:messages", domain))
                |> ignore)))
        .AddSingleton<IAuthorizationHandler, HasScopeHandler>()
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer (fun options ->
            options.Authority <- domain
            options.Audience <- audience
            #if DEBUG
            options.RequireHttpsMetadata <- false // only set to false in development
            #endif
            options.TokenValidationParameters <- TokenValidationParameters(NameClaimType = ClaimTypes.NameIdentifier)
        )
        |> ignore

    services

let app =
    application {
        use_router webApp
        url ("http://*:" + port)
        service_config configureServices
        app_config configureApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
