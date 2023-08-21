module App.Controller

open System.Text.Json
open App.Domain
open App.ViewModel
open Falco

let CreatePessoaHandler (dbCtx:PessoaDbContext) redis =
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
        Cache.addPerson redis (ViewPessoa.FromPessoa pessoa)
        CreatePessoa dbCtx pessoa |> ignore
        (Response.withStatusCode 201
         >> Response.withHeaders [ ("Location", $"/pessoas/{pessoa.Id}") ]
         >> Response.ofEmpty)
    try
        Request.mapJson handleCreatePessoa
    with
    | :? JsonException as ex -> (Response.withStatusCode 400 >> Response.ofPlainText ex.Message)

let GetPessoaHandler (dbCtx:PessoaDbContext) : HttpHandler =
    let getPessoa = GetPessoa dbCtx
    fun ctx ->
        let r = Request.getRoute ctx
        let id = r.GetGuid "id"
        let maybePessoa = getPessoa id
        match maybePessoa with
        | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson (ViewPessoa.FromPessoa pessoa)) ctx
        | None ->  (Response.withStatusCode 404 >> Response.ofEmpty) ctx
    
let SearchPessoasHandler (dbCtx:PessoaDbContext) redis : HttpHandler =
    let searchPessoa = Cache.searchPerson redis 
    fun ctx ->
        let r = Request.getQuery ctx
        let query = r.GetString "t"
        match query with
        | null 
        | "" -> (Response.withStatusCode 400 >> Response.ofEmpty) ctx
        | query ->
        let pessoas = searchPessoa query
        (Response.withStatusCode 200 >> Response.ofJson pessoas) ctx
    
let CountPessoasHandler (dbCtx:PessoaDbContext) : HttpHandler = fun ctx ->
    let count = CountPessoas dbCtx
    (Response.withStatusCode 200 >> Response.ofPlainText (count.ToString())) ctx