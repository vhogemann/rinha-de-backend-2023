module App.Queue

open Npgsql

type IPessoaInsertQueue =
    abstract enqueue:Domain.Pessoa -> unit

type Message =
        | Insert of Domain.Pessoa
        | Flush

type PessoaInsertQueue (db:NpgsqlConnection ) =
    
    let agent = MailboxProcessor<Message>.Start( fun inbox ->
        let rec loop(queue) = async {
            let! message = inbox.Receive()
            match message with
            | Insert pessoa ->
                let queue = List.append queue [pessoa]
                if queue |> Seq.length >= 200 then
                    do! Domain.insertBatch db queue
                    return! loop(List.Empty)
                else
                    return! loop(queue)
            | Flush ->
                do! Domain.insertBatch db queue
                return! loop(List.Empty)
        }
        loop(List.Empty)
    )
    
    interface IPessoaInsertQueue with
        member _.enqueue(pessoa:Domain.Pessoa) =
            Insert pessoa |> agent.Post 