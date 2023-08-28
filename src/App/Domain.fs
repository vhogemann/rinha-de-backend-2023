﻿module App.Domain

open System
open System.Data
open System.Text.Json
open System.Text.Json.Serialization
open Donald
open Npgsql

type Pessoa = {
    Id: Guid
    Apelido: string
    Nome: string
    Nascimento: DateTime
    Stack: String[]
}
module Pessoa =
    let ofDataReader (rd:IDataReader): Pessoa =
        {
            Id = rd.ReadGuid "Id"
            Apelido = rd.ReadString "Apelido"
            Nome = rd.ReadString "Nome"
            Nascimento = rd.ReadDateTime "Nascimento" 
            Stack = rd.ReadString "Stack" |> JsonSerializer.Deserialize<string[]>
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
        "Id", sqlGuid person.Id
        "Apelido", sqlString person.Apelido
        "Nome", sqlString person.Nome
        "Nascimento", sqlDateTime (person.Nascimento)
        "Stack", sqlString (person.Stack |> JsonSerializer.Serialize)
    ]
    
    conn
    |> Db.newCommand sql
    |> Db.setParams param
    |> Db.Async.exec
    |> Async.AwaitTask

let insertBatch (conn:NpgsqlConnection) pessoas =
    let sql = """ INSERT INTO "Pessoas" VALUES (@Id, @Apelido, @Nome, @Nascimento, @Stack) """
    
    let param =
        seq {
            for pessoa in pessoas do
                yield [
                    "Id", sqlGuid pessoa.Id
                    "Apelido", sqlString pessoa.Apelido
                    "Nome", sqlString pessoa.Nome
                    "Nascimento", sqlDateTime (pessoa.Nascimento)
                    "Stack", sqlString (pessoa.Stack |> JsonSerializer.Serialize)
                ]
        }
        |> List.ofSeq
    conn
    |> Db.newCommand sql
    |> Db.Async.execMany param
    |> Async.AwaitTask    

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
    |> Db.querySingle Pessoa.ofDataReader

let apelidoExists (conn:NpgsqlConnection) apelido =
    let sql = """
        SELECT count(*) FROM "Pessoas" WHERE "Apelido" = @Apelido
    """
    let param = [
        "Apelido", sqlString apelido
    ]
    let count = 
        conn
        |> Db.newCommand sql
        |> Db.setParams param
        |> Db.scalar unbox<Int64>
    count > 0L
    
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
    |> Db.query Pessoa.ofDataReader
    
let count (conn:NpgsqlConnection) =
    let sql = "SELECT count(*) From \"Pessoas\""
    
    conn
    |> Db.newCommand sql
    |> Db.scalar unbox<Int64> 