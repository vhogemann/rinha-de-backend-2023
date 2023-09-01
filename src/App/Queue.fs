module App.Queue

open System.Threading.Tasks.Dataflow
open App.Cache
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Control
open Npgsql

type IPessoaInsertQueue =
    abstract Enqueue:Domain.Pessoa -> unit
    abstract Flush:unit -> unit

type Message =
        | Insert of Domain.Pessoa
        | Flush

type PessoaInsertQueue (logger:ILogger<PessoaInsertQueue>, db:NpgsqlConnection, cache:IApelidoCache) =
    let tryInsert batch loop = task {
        try
            let! result = Domain.insertBatch logger db batch
            logger.LogInformation("Inserted batch of {0}", result)
            logger.LogInformation("Inserted batch of {0}", batch |> Seq.length)
            return! loop List.Empty
        with exp ->
            logger.LogError(exp, "Error inserting batch of {0}", batch |> Seq.length)
            return! loop (batch |> List.filter ( cache.Exists >> not ))
    }
        
    let agent = 
        MailboxProcessor<Message>.Start( fun inbox ->
            let rec loop batch = task {
                let! message = inbox.Receive()
                match message with
                | Insert pessoa ->
                    let batch = List.append batch [pessoa]
                    if batch |> Seq.length >= 500 then
                        return! tryInsert batch loop
                    else
                        return! loop batch
                | Flush ->
                    if not (List.isEmpty batch) then
                        return! tryInsert batch loop
                    else
                        return! loop(batch)
            }
            loop(List.Empty) |> Async.AwaitTask
        )

    let _ = 
        async {
            while true do
                do! Async.Sleep 2000
                agent.Post Flush
        } |> Async.Start
    
    interface IPessoaInsertQueue with
        member _.Enqueue(pessoa:Domain.Pessoa) =
            Insert pessoa |> agent.Post
            
        member _.Flush() =
            agent.Post Flush
            