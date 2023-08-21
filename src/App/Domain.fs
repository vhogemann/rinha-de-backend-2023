﻿module App.Domain

open System
open System.Data
open System.Text.Json
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

let insert (conn:NpgsqlConnection) person =
    let sql = "INSERT INTO Pessoa (Id, Apelido, Nome, Nascimento, Stack)"
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
    |> Db.exec
    
let fetch (conn:NpgsqlConnection) id =
    let sql = "SELECT Id, Apelido, Nome, Nascimento, Stack FROM Pessoa"
    let param = [
        "Id", sqlGuid id
    ]
    
    conn
    |> Db.newCommand sql
    |> Db.setParams param
    |> Db.querySingle Pessoa.ofDataReader
    
let search (conn:NpgsqlConnection) termo =
    let sql = """
    SELECT
      Id, Apelido, Nome, Nascimento, Stack 
    FROM 
      Pessoas 
    WHERE 
      BUSCA ILIKE '%' || @Termo || '%'
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
    let sql = "SELECT count(*) From Pessoas"
    
    conn
    |> Db.newCommand sql
    |> Db.scalar (fun it -> it :? Int64) 