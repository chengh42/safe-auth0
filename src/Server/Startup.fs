module Startup

// See: https://auth0.com/docs/quickstart/backend/aspnet-core-webapi

open System
open System.Security.Claims
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
open Giraffe

[<RequireQualifiedAccess>]
module Auth0 =
    let [<Literal>] domain = "dev-nik3xlx8.us.auth0.com"
    let [<Literal>] audience = "https://safe-auth0.herokuapp.com/api/"

type Startup(cfg:IConfiguration, env:IWebHostEnvironment) =
    member __.ConfigureServices (services: IServiceCollection) =
        let domain = $"https://{Auth0.domain}/";
        let mvcOptions =
            Action<MvcOptions>(fun opts ->
                opts.EnableEndpointRouting <- false)
        services
            .AddMvc()
            .AddMvcOptions(mvcOptions)
            |> ignore

        // add authentication services
        services
            .AddAuthentication(fun opts ->
                opts.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                opts.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme
            )
            .AddJwtBearer (fun options ->
                options.Authority <- domain
                options.Audience <- Auth0.audience
            )
            |> ignore
        services
            .AddGiraffe()
            |> ignore

    member __.Configure (app: IApplicationBuilder, env: IWebHostEnvironment) =

        match env.EnvironmentName with
        | "development" -> app.UseDeveloperExceptionPage()
        | _ -> app.UseExceptionHandler "/Home/Error"
        |> ignore

        let configureRoutes =
            Action<IRouteBuilder>(fun routes ->
                routes.MapRoute(name="default", template="{controller=Home}/{action=Index}/{id?}")
                |> ignore)

        app
            .UseStaticFiles()
            .UseAuthentication() // enable authentication middleware
            .UseMvc(configureRoutes)
            // .UseAuthorization()
            .UseGiraffe WebApp.webApp
            |> ignore
