module App.Controller

open System.Text.Json.Serialization
open App
open Falco
open System.Text.Json

module CreatePessoa =
    let deserialize:string->Result<ViewModel.CreatePessoa, int> =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options.AllowTrailingCommas <- true
        options.PropertyNameCaseInsensitive <- true
        fun json ->
            try 
                JsonSerializer.Deserialize<ViewModel.CreatePessoa> json |> Ok
            with exp ->
                Error 400
    let exists db (pessoa:ViewModel.CreatePessoa) =
        if Domain.apelidoExists db pessoa.Apelido then
            Error 422
        else
            Ok pessoa
    
    let insert queue pessoa =
        try
            queue pessoa
            Ok pessoa            
        with exp ->
            Error 500
            
    let handler db =
        let queue =
            Domain.queueInsert (Domain.insert db)
            
        fun pessoa ->
            pessoa
            |> deserialize
            |> Result.bind (exists db)
            |> Result.bind (ViewModel.asPessoa)
            |> Result.bind (insert queue)
            |> function
                | Error status ->
                    (Response.withStatusCode status >> Response.ofEmpty)
                | Ok pessoa ->
                    (Response.withStatusCode 201
                     >> Response.withHeaders [ ("Location", $"/pessoas/{pessoa.Id}") ]
                     >> Response.ofEmpty)
let CreatePessoaHandler db :HttpHandler = Request.bodyString (CreatePessoa.handler db)

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