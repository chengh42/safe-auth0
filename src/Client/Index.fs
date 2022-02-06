module Index

open Elmish
open Fable.Remoting.Client
open Shared

type Model = { Todos: Todo list; Input: string }

type Msg =
    | GotTodos of Todo list
    | SetInput of string
    | AddTodo
    | AddedTodo of Todo

let todosApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init () : Model * Cmd<Msg> =
    let model = { Todos = []; Input = "" }

    let cmd =
        Cmd.OfAsync.perform todosApi.getTodos () GotTodos

    model, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodos todos -> { model with Todos = todos }, Cmd.none
    | SetInput value -> { model with Input = value }, Cmd.none
    | AddTodo ->
        let todo = Todo.create model.Input

        let cmd =
            Cmd.OfAsync.perform todosApi.addTodo todo AddedTodo

        { model with Input = "" }, cmd
    | AddedTodo todo ->
        { model with
              Todos = model.Todos @ [ todo ] },
        Cmd.none

open Fable.Core
open Fable.Auth0.React
open Feliz
open Feliz.Bulma
open Feliz.Bulma.Operators

let auth0App (children: seq<ReactElement>) =
    let opts =
        unbox<Auth0ProviderOptions>
            {| domain = "dev-nik3xlx8.us.auth0.com"
               clientId = "t81FqAoSB0LQn0tgCDSNG0u7z34oC0SW"
               redirectUri = Browser.Dom.window.location.origin
               audience = "https://dev-nik3xlx8.us.auth0.com/api/v2/"
               scope = "read:current_user update:current_user_metadata" |}
    Auth0Provider opts children


[<ReactComponent>]
let AuthenticationBox () =
    let ctxAuth0 = useAuth0 ()

    // correspond to authentication method `loginWithRedirect()` in JS
    let handleLoginWithRedirect _ =
        let opts = unbox<RedirectLoginOptions> null
        ctxAuth0.loginWithRedirect opts
        |> Async.AwaitPromise
        |> Async.StartImmediate

    // correspond to authentication method `logout({ returnTo: window.location.href })` in JS
    let handleLogoutWithRedirect _ =
        let returnTo = Browser.Dom.window.location.href
        let opts = unbox<LogoutOptions> {| returnTo = returnTo |}
        ctxAuth0.logout opts

    let loginButton =
        Bulma.button.button [
            color.isPrimary
            prop.onClick handleLoginWithRedirect
            prop.text "Login"
        ]

    let logoutButton =
        Bulma.button.button [
            color.isPrimary
            prop.onClick handleLogoutWithRedirect
            prop.children [
                Html.span "Logout"
                Bulma.icon [ Html.i [ prop.className "fas fa-sign-out-alt" ] ]
            ]
        ]

    match ctxAuth0.isLoading, ctxAuth0.user with
    | _, None ->
        // if not signed in, show login button
        loginButton
    | true, _ ->
        Bulma.icon [
            icon.isLarge
            prop.children [
                Html.i [ prop.className "fas fa-spinner" ]
            ]
        ]
    | false, Some (user: User) ->
        // if signed in, show user profile and logout button
        let username, picture, usersub =
            $"{user.name}", $"{user.picture}", $"{user.sub}"

        let userProfile =
            Bulma.level [
                color.hasBackgroundPrimary
                prop.style [ style.padding (length.em 0.5) ]
                prop.children [
                    Bulma.levelItem [
                        Bulma.image [
                            Bulma.image.isRounded
                            ++ Bulma.image.is48x48
                            prop.children [
                                Html.img [
                                    prop.style [ style.maxHeight.unset ]
                                    prop.alt username
                                    prop.src picture
                                ]
                            ]
                        ]
                    ]
                    Bulma.levelItem [
                        Bulma.title.p [
                            Bulma.title.is5
                            prop.text username
                        ]
                    ]
                ]
            ]

        Bulma.level [
            Bulma.levelLeft [
                Bulma.levelItem [
                    userProfile
                ]
                Bulma.levelItem [
                    Bulma.field.div [
                        Bulma.field.hasAddons
                        prop.style [ style.marginLeft (length.em 1) ]
                        prop.children [
                            Bulma.control.p [ logoutButton ]
                        ]
                    ]
                ]
            ]
        ]

let navBrand =
    Bulma.navbarBrand.div [
        Bulma.navbarItem.a [
            prop.href "https://safe-stack.github.io/"
            navbarItem.isActive
            prop.children [
                Html.img [
                    prop.src "/favicon.png"
                    prop.alt "Logo"
                ]
            ]
        ]
        Bulma.navbarItem.div [
            AuthenticationBox ()
        ]
    ]

let containerBox (model: Model) (dispatch: Msg -> unit) =
    Bulma.box [
        Bulma.content [
            Html.ol [
                for todo in model.Todos do
                    Html.li [ prop.text todo.Description ]
            ]
        ]
        Bulma.field.div [
            field.isGrouped
            prop.children [
                Bulma.control.p [
                    control.isExpanded
                    prop.children [
                        Bulma.input.text [
                            prop.value model.Input
                            prop.placeholder "What needs to be done?"
                            prop.onChange (fun x -> SetInput x |> dispatch)
                        ]
                    ]
                ]
                Bulma.control.p [
                    Bulma.button.a [
                        color.isPrimary
                        prop.disabled (Todo.isValid model.Input |> not)
                        prop.onClick (fun _ -> dispatch AddTodo)
                        prop.text "Add"
                    ]
                ]
            ]
        ]
    ]


let view (model: Model) (dispatch: Msg -> unit) =
    auth0App [
        Bulma.hero [
            hero.isFullHeight
            color.isPrimary
            prop.style [
                style.backgroundSize "cover"
                style.backgroundImageUrl "https://unsplash.it/1200/900?random"
                style.backgroundPosition "no-repeat center center fixed"
            ]
            prop.children [
                Bulma.heroHead [
                    Bulma.navbar [
                        Bulma.container [ navBrand ]
                    ]
                ]
                Bulma.heroBody [
                    Bulma.container [
                        Bulma.column [
                            column.is6
                            column.isOffset3
                            prop.children [
                                Bulma.title [
                                    text.hasTextCentered
                                    prop.text "safe_auth0"
                                ]
                                containerBox model dispatch
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
