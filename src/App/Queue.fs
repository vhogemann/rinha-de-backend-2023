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
                let queue = Seq.append queue [pessoa]
                return! loop(List.Empty)
            | Flush ->
                return! loop(queue)
        }
        loop(List.Empty)
    )
    interface IPessoaInsertQueue with
        member _.enqueue(pessoa:Domain.Pessoa) =
            Insert pessoa |> agent.Post 