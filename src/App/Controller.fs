module App.Controller

open System.Net.Http
open System.Text.Json
open App.ViewModel
open Falco
open Microsoft.AspNetCore.Http

let CreatePessoaHandler db :HttpHandler =
    let apelidoExists = Domain.apelidoExists db
    
    let handleCreatePessoa ctx (createPessoa:CreatePessoa) =
        match asPessoa createPessoa with
        | Error message ->
            (Response.withStatusCode 400 >> Response.ofPlainText message) ctx
        | Ok pessoa ->
        
        if (apelidoExists pessoa.Apelido) then
            (Response.withStatusCode 422 >> Response.ofEmpty) ctx
        else
        Domain.insert db pessoa
        (Response.withStatusCode 201
         >> Response.withHeaders [ ("Location", $"/pessoas/{pessoa.Id}") ]
         >> Response.ofEmpty) ctx
    
    fun ctx -> 
        try
           ctx.Request.Body |> JsonSerializer.DeserializeAsync<CreatePessoa>
           |> fun task -> task.AsTask()
           |> Async.AwaitTask
           |> Async.RunSynchronously
           |> handleCreatePessoa ctx
        with exp ->
            (Response.withStatusCode 400 >> Response.ofEmpty) ctx
            
let GetPessoaHandler db : HttpHandler =
    fun ctx ->
        let r = Request.getRoute ctx
        let id = r.GetGuid "id"
        let maybePessoa = Domain.fetch db id
        match maybePessoa with
        | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson pessoa) ctx
        | None ->  (Response.withStatusCode 404 >> Response.ofEmpty) ctx
    
let SearchPessoasHandler db : HttpHandler =
    fun ctx ->
        let r = Request.getQuery ctx
        let query = r.GetString "t"
        match query with
        | null 
        | "" -> (Response.withStatusCode 400 >> Response.ofEmpty) ctx
        | query ->
        let pessoas =
            Domain.search db query
        (Response.withStatusCode 200 >> Response.ofJson pessoas) ctx
    
let CountPessoasHandler db : HttpHandler = fun ctx ->
    let count = Domain.count db
    (Response.withStatusCode 200 >> Response.ofPlainText (count.ToString())) ctx