module App.Domain

open System
open System.Data
open System.Text.Json
open System.Text.Json.Serialization
open Donald
open Microsoft.Extensions.Logging
open Npgsql

type Pessoa = {
    id: Guid
    apelido: string
    nome: string
    nascimento: DateTime
    stack: String[]
}
module Pessoa =
    let ofDataReader (rd:IDataReader): Pessoa =
        {
            id = rd.ReadGuid "Id"
            apelido = rd.ReadString "Apelido"
            nome = rd.ReadString "Nome"
            nascimento = rd.ReadDateTime "Nascimento" 
            stack = rd.ReadString "Stack" |> JsonSerializer.Deserialize<string[]>
        }

let JsonOptions =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    options.AllowTrailingCommas <- true
    options.PropertyNameCaseInsensitive <- true
    options

let insert (conn:NpgsqlConnection) person =
    let sql = """
        INSERT INTO "Pessoas" VALUES (@Id, @Apelido, @Nome, @Nascimento, @Stack)
    """

    let param = [
        "Id", sqlGuid person.id
        "Apelido", sqlString person.apelido
        "Nome", sqlString person.nome
        "Nascimento", sqlDateTime (person.nascimento)
        "Stack", sqlString (person.stack |> JsonSerializer.Serialize)
    ]
    
    conn
    |> Db.newCommand sql
    |> Db.setParams param
    |> Db.Async.exec

let insertBatch (logger:ILogger<'T>) (conn:NpgsqlConnection) pessoas =
    let sql = """ INSERT INTO "Pessoas" VALUES (@Id, @Apelido, @Nome, @Nascimento, @Stack) """
    use tran = conn.TryBeginTransaction()
    
    
    
let queueInsert insert =
    let agent = MailboxProcessor<Pessoa>.Start( fun inbox ->
        let rec loop() = async {
           let! pessoa = inbox.Receive()
           try
            do! insert pessoa
           with exp ->
               Console.Out.WriteLine (exp.Message)
           do! loop() 
        }
        loop()
    )
    agent.Post

let fetch (conn:NpgsqlConnection) id =
    let sql = """
    SELECT "Id", "Apelido", "Nome", "Nascimento", "Stack" FROM "Pessoas"
    """
    let param = [
        "Id", sqlGuid id
    ]
    
    conn
    |> Db.newCommand sql
    |> Db.setParams param
    |> Db.Async.querySingle Pessoa.ofDataReader

let apelidoExists (conn:NpgsqlConnection) apelido = task {
    let sql = """
        SELECT count(*) FROM "Pessoas" WHERE "Apelido" = @Apelido
    """
    let param = [
        "Apelido", sqlString apelido
    ]
    let! count = 
        conn
        |> Db.newCommand sql
        |> Db.setParams param
        |> Db.Async.scalar unbox<Int64>
    return count > 0L
}
    
let search (conn:NpgsqlConnection) termo =
    let sql = """
    SELECT
      "Id", "Apelido", "Nome", "Nascimento", "Stack" 
    FROM 
      "Pessoas" 
    WHERE 
      "Busca" ILIKE '%' || @Termo || '%'
      limit 50;
    """
    let param = [
        "Termo", sqlString termo
    ]
    
    conn
    |> Db.newCommand sql
    |> Db.setParams param
    |> Db.Async.query Pessoa.ofDataReader
    
let count (conn:NpgsqlConnection) =
    let sql = "SELECT count(*) From \"Pessoas\""
    
    conn
    |> Db.newCommand sql
    |> Db.Async.scalar unbox<Int64> 
    