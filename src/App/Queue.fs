module App.Queue

open Microsoft.FSharp.Control
open Npgsql

type IPessoaInsertQueue =
    abstract enqueue:Domain.Pessoa -> unit
    abstract flush:unit -> unit

type Message =
        | Insert of Domain.Pessoa
        | Flush

type PessoaInsertQueue (db:NpgsqlConnection ) =
    let agent = 
        MailboxProcessor<Message>.Start( fun inbox ->
            let rec loop batch = async {
                let! message = inbox.Receive()
                match message with
                | Insert pessoa ->
                    let batch = List.append batch [pessoa]
                    if batch |> Seq.length >= 200 then
                        do! Domain.insertBatch db batch
                        return! loop(List.Empty)
                    else
                        return! loop(batch)
                | Flush ->
                    if not (List.isEmpty batch) then
                        do! Domain.insertBatch db batch
                        return! loop(List.Empty)
                    else
                        return! loop(batch)
            }
            loop(List.Empty)
        )

    let _ = 
        async {
            while true do
                do! Async.Sleep 1000
                agent.Post Flush
        } |> Async.Start
    
    interface IPessoaInsertQueue with
        member _.enqueue(pessoa:Domain.Pessoa) =
            Insert pessoa |> agent.Post
            
        member _.flush() =
            agent.Post Flush
            