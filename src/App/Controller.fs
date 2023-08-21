module App.Controller

open System.Text.Json
open App.ViewModel
open Falco

let CreatePessoaHandler db redis =
    let apelidoExists apelido =
        Cache.get redis apelido |> Option.isSome
    
    let handleCreatePessoa (createPessoa:CreatePessoa) : HttpHandler =
        match asPessoa createPessoa with
        | Error message ->
            (Response.withStatusCode 400 >> Response.ofPlainText message)
        | Ok pessoa ->
        
        if (apelidoExists pessoa.Apelido) then
            (Response.withStatusCode 422 >> Response.ofEmpty)
        else
        Domain.insert db pessoa
        (Response.withStatusCode 201
         >> Response.withHeaders [ ("Location", $"/pessoas/{pessoa.Id}") ]
         >> Response.ofEmpty)
    try
        Request.mapJson handleCreatePessoa
    with
    | :? JsonException as ex -> (Response.withStatusCode 400 >> Response.ofPlainText ex.Message)

let GetPessoaHandler db : HttpHandler =
    fun ctx ->
        let r = Request.getRoute ctx
        let id = r.GetGuid "id"
        let maybePessoa = Domain.fetch db id
        match maybePessoa with
        | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson pessoa) ctx
        | None ->  (Response.withStatusCode 404 >> Response.ofEmpty) ctx
    
let SearchPessoasHandler db redis : HttpHandler =
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