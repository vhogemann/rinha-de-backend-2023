module App.Domain

open System
open System.Data
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open Donald
open Npgsql

type Pessoa = {
    id: Guid
    apelido: string
    nome: string
    nascimento: DateTime
    stack: String[]
}

let JsonOptions =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    options.AllowTrailingCommas <- true
    options.PropertyNameCaseInsensitive <- true
    options

module Pessoa =
    let ofDataReader (rd:IDataReader): Pessoa =
        {
            id = rd.ReadGuid "Id"
            apelido = rd.ReadString "Apelido"
            nome = rd.ReadString "Nome"
            nascimento = rd.ReadDateTime "Nascimento" 
            stack = rd.ReadString "Stack" |> JsonSerializer.Deserialize<string[]>
        }

    let exists (conn:NpgsqlConnection) (apelido:string) =
        let sql = """ SELECT count(*) FROM "Pessoas" WHERE "Apelido" = @Apelido """
        let param = [
            "Apelido", sqlString apelido
        ]
        task {
            let! count =
                conn
                |> Db.newCommand sql
                |> Db.setParams param
                |> Db.Async.scalar unbox<Int64>
            return count > 0L
        }
    
    let insertBatch (conn:NpgsqlConnection) (pessoas:Pessoa seq) =
        let sql = """ INSERT INTO "Pessoas" VALUES (@Id, @Apelido, @Nome, @Nascimento, @Stack) """
        let param =
            seq {
                for pessoa in pessoas do
                    yield [
                        "Id", sqlGuid pessoa.id
                        "Apelido", sqlString pessoa.apelido
                        "Nome", sqlString pessoa.nome
                        "Nascimento", sqlDateTime (pessoa.nascimento)
                        "Stack", sqlString (pessoa.stack |> JsonSerializer.Serialize)
                    ]
            }
            |> List.ofSeq
        task {
            use tran = conn.TryBeginTransaction()
            do!
                conn
                |> Db.newCommand sql
                |> Db.setTransaction tran
                |> Db.Async.execMany param
                |> Async.AwaitTask
            tran.TryCommit()
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
        |> Db.Async.query ofDataReader
    
    let count (conn:NpgsqlConnection) =
        let sql = "SELECT count(*) From \"Pessoas\""
        conn
        |> Db.newCommand sql
        |> Db.Async.scalar unbox<Int64>
    
type IRepository =
    abstract member Insert : Pessoa seq -> Task
    abstract member Search : string -> Task<Pessoa list>
    abstract member Count : unit -> Task<Int64>
    abstract member Exists: string -> Task<bool>
    
type Repository (conn:NpgsqlConnection) =
    interface IRepository with
        member _.Insert pessoas = Pessoa.insertBatch conn pessoas
        member _.Search termo = Pessoa.search conn termo
        member _.Count() = Pessoa.count conn
        member _.Exists apelido = Pessoa.exists conn apelido