﻿module App.Controller

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
                        
    let existsOnCache (cache:Cache.IPessoaCache) pessoa =
        if cache.Exists pessoa then
            Error (422, "Apelido existe")
        else
            Ok pessoa
    
    let handler =
        Services.inject<Queue.IPessoaInsertQueue, Cache.IPessoaCache, Domain.IRepository> (
            fun queue cache repository->
                fun ctx -> task {
                    let! json = Request.getBodyString ctx
                    let pessoa =
                       json
                       |> deserialize
                       |> Result.bind ViewModel.asPessoa
                       |> Result.bind (existsOnCache cache)
                    match pessoa with
                    | Error (code, message) ->
                        return! (Response.withStatusCode code >> Response.ofPlainText message) ctx
                    | Ok pessoa ->
                        do! cache.Add pessoa
                        queue.Enqueue pessoa
                        return! (Response.withStatusCode 201 >>
                                 Response.withHeaders [ "Location",  $"/pessoas/{pessoa.id}" ] >>
                                 Response.ofEmpty) ctx
                }
        )

module GetPessoa =
    let handler =
        Services.inject<Cache.IPessoaCache, Domain.IRepository>( fun cache repo ->
            fun ctx -> task {
                let route = Request.getRoute ctx
                let id = route.GetGuid "id"
                match cache.Get id with
                | Some pessoa ->
                    return! (Response.withStatusCode 200 >> Response.ofJson pessoa) ctx
                | None ->
                    let! pessoa = repo.Get id
                    match pessoa with
                    | Some pessoa ->
                        return! (Response.withStatusCode 200 >> Response.ofJson pessoa) ctx
                    | None ->
                        return! (Response.withStatusCode 404 >> Response.ofEmpty) ctx
            }
        )

module SearchPessoa =
    let handler =
        Services.inject<Domain.IRepository>( fun repo ->
            fun ctx -> task {
                let r = Request.getQuery ctx
                let query = r.GetString "t"
                match query with
                | null 
                | "" -> return! (Response.withStatusCode 400 >> Response.ofEmpty) ctx
                | query ->
                    let! pessoas = repo.Search query
                    return!  (Response.withStatusCode 200 >> Response.ofJson pessoas) ctx
            }
        )

module CountPessoas =
    let handler =
        Services.inject<Domain.IRepository>( fun repo ->
            fun ctx -> task {
                let! count = repo.Count()
                return! (Response.withStatusCode 200 >> Response.ofPlainText (count.ToString())) ctx
            }
        )