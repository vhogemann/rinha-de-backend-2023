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
                        
    let existsOnCache (cache:Cache.IApelidoCache) pessoa =
        if cache.Test pessoa then
            Error (422, "Apelido existe")
        else
            Ok pessoa
    
    let handler  (queue:Queue.IPessoaInsertQueue) (pessoaCache:Cache.IPessoaCache) (apelidoCache:Cache.IApelidoCache) =
        let enqueue pessoa =
            pessoaCache.Add pessoa
            apelidoCache.Add pessoa
            queue.Enqueue pessoa

        fun pessoa ->
            pessoa
            |> deserialize
            |> Result.bind ViewModel.asPessoa
            |> Result.bind (existsOnCache apelidoCache)
            |> function
                | Error (status, message) ->
                    (Response.withStatusCode status >> Response.ofPlainText message)
                | Ok pessoa ->
                    enqueue pessoa
                    (Response.withStatusCode 201
                     >> Response.withHeaders [ ("Location", $"/pessoas/{pessoa.id}") ]
                     >> Response.ofEmpty)
let CreatePessoaHandler queue pessoaCache apelidoCache :HttpHandler = Request.bodyString (CreatePessoa.handler queue pessoaCache apelidoCache)

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

module SearchPessoas =
    let search (db:Npgsql.NpgsqlConnection) term =
        Domain.search db term


let SearchPessoasHandler db : HttpHandler =
    fun ctx ->
        let r = Request.getQuery ctx
        let query = r.GetString "t"
        match query with
        | null 
        | "" -> (Response.withStatusCode 400 >> Response.ofEmpty) ctx
        | query ->
        let pessoas =
            SearchPessoas.search db query
        (Response.withStatusCode 200 >> Response.ofJson pessoas) ctx
    
let CountPessoasHandler db : HttpHandler = fun ctx ->
    let count = Domain.count db
    (Response.withStatusCode 200 >> Response.ofPlainText (count.ToString())) ctx