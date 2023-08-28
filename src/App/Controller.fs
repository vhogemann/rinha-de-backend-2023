module App.Controller

open System.Text.Json.Serialization
open App
open Falco
open System.Text.Json

module CreatePessoa =
    let deserialize:string->Result<ViewModel.CreatePessoa, int*string> =
        fun json ->
            try 
                JsonSerializer.Deserialize<ViewModel.CreatePessoa>(json, Domain.JsonOptions)|> Ok
            with exp ->
                Error (400, exp.Message)

    let existsOnCache (cache:Cache.IPessoaCache) (pessoa:Domain.Pessoa)=
        match cache.GetByApelido (pessoa.Apelido) with
        | Some _ ->
            Error (422, "Apelido existe")
        | None ->
            Ok pessoa
        
    let existsOnDb db (pessoa:Domain.Pessoa) =
        if Domain.apelidoExists db (pessoa.Apelido) then
            Error (422, "Apelido existe")
        else
            Ok pessoa
    
    let handler db (queue:Queue.IPessoaInsertQueue) (cache:Cache.IPessoaCache) =
        let enqueue pessoa =
            queue.enqueue pessoa
            pessoa
        fun pessoa ->
            pessoa
            |> deserialize
            |> Result.bind (ViewModel.asPessoa)
            |> Result.bind (existsOnCache cache)
            |> Result.bind (existsOnDb db)
            |> Result.map enqueue
            |> function
                | Error (status, message) ->
                    (Response.withStatusCode status >> Response.ofPlainText message)
                | Ok pessoa ->
                    cache.Add pessoa
                    (Response.withStatusCode 201
                     >> Response.withHeaders [ ("Location", $"/pessoas/{pessoa.Id}") ]
                     >> Response.ofEmpty)
let CreatePessoaHandler db queue cache :HttpHandler = Request.bodyString (CreatePessoa.handler db queue cache)

module GetPessoa =
    let getId = Request.mapRoute (fun r -> r.GetGuid "id")
    let getPessoa (cache:Cache.IPessoaCache) db id =
        match cache.Get id with
        | Some person -> Some person
        | None -> Domain.fetch db id
    
    let mapResponse =
        function
        | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson pessoa)
        | None -> (Response.withStatusCode 404 >> Response.ofEmpty)
    
let GetPessoaHandler db cache : HttpHandler =
    GetPessoa.getId (GetPessoa.getPessoa cache db >> GetPessoa.mapResponse)
    
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