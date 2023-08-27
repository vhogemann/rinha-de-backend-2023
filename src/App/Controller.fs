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
    let exists db (pessoa:ViewModel.CreatePessoa) =
        if Domain.apelidoExists db pessoa.Apelido then
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
            |> Result.bind (exists db)
            |> Result.bind (ViewModel.asPessoa)
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
                
    let getPessoa db id =
        match Domain.fetch db id with
        | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson pessoa)
        | None -> (Response.withStatusCode 404 >> Response.ofEmpty)
    
let GetPessoaHandler db : HttpHandler = GetPessoa.getId (GetPessoa.getPessoa db)
    
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