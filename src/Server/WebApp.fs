module WebApp

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer

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

storage.AddTodo(Todo.create "Create new SAFE project") |> ignore
storage.AddTodo(Todo.create "Write your app") |> ignore
storage.AddTodo(Todo.create "Ship it !!!") |> ignore

let todosApi =
    let implementation =
        { getTodos = fun () -> async { return storage.GetTodos() }
          addTodo =
            fun todo ->
                async {
                    match storage.AddTodo todo with
                    | Ok () -> return todo
                    | Error e -> return failwith e
                } }

    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue implementation
    |> Remoting.buildHttpHandler

let webApp : HttpHandler =
    choose [
        todosApi
        htmlFile "public/index.html"
    ]