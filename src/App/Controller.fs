module App.Controller

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
        if cache.Exists pessoa then
            Error (422, "Apelido existe")
        else
            Ok pessoa

let CreatePessoaHandler (queue:Queue.IPessoaInsertQueue) (apelidoCache:Cache.IApelidoCache) :HttpHandler =
    fun ctx -> task {
        let! json = Request.getBodyString ctx
        let pessoa =
           json
           |> CreatePessoa.deserialize
           |> Result.bind ViewModel.asPessoa
           |> Result.bind (CreatePessoa.existsOnCache apelidoCache)
        
        match pessoa with
        | Error (code, message) ->
            return! (Response.withStatusCode code >> Response.ofPlainText message) ctx
        | Ok pessoa ->
            do! apelidoCache.Add pessoa
            queue.Enqueue pessoa
            return! (Response.withStatusCode 201 >>
                     Response.withHeaders [ "Location",  $"/pessoas/{pessoa.id}" ] >>
                     Response.ofEmpty) ctx
    }

module GetPessoa =
    let mapResponse =
        function
        | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson pessoa)
        | None -> (Response.withStatusCode 404 >> Response.ofEmpty)
    
let GetPessoaHandler db : HttpHandler =
    fun ctx -> task {
        let route = Request.getRoute ctx
        let id = route.GetGuid "id"
        let! pessoa = Domain.fetch db id
        return! (GetPessoa.mapResponse pessoa ctx)
    }

let SearchPessoasHandler db : HttpHandler =
    fun ctx -> task {
        let r = Request.getQuery ctx
        let query = r.GetString "t"
        match query with
        | null 
        | "" -> return! (Response.withStatusCode 400 >> Response.ofEmpty) ctx
        | query ->
            let! pessoas = Domain.search db query
            return!  (Response.withStatusCode 200 >> Response.ofJson pessoas) ctx
    }
    
let CountPessoasHandler db : HttpHandler = fun ctx -> task {
        let! count = Domain.count db
        return! (Response.withStatusCode 200 >> Response.ofPlainText (count.ToString())) ctx
    }