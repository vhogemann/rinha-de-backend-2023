module App.Queue

open Npgsql

type IPessoaInsertQueue =
    abstract enqueue:Domain.Pessoa -> unit

type PessoaInsertQueue (db:NpgsqlConnection ) =
    let agent = MailboxProcessor<Domain.Pessoa>.Start( fun inbox ->
        let rec loop() = async {
            let! pessoa = inbox.Receive()
            do! Domain.insert db pessoa
            return! loop()
        }
        loop()
    )
    interface IPessoaInsertQueue with
        member _.enqueue(pessoa:Domain.Pessoa) =
            agent.Post pessoa