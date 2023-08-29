module App.Cache

open System
open System.Text.Json
open System.Threading.Tasks
open NRedisStack.Search
open NRedisStack.Search.Literals.Enums
open NRedisStack.RedisStackCommands
open StackExchange.Redis.MultiplexerPool

let getJson (redis:IConnectionMultiplexerPool) (key:string) : Task<'T option> = task {
    let! pool = redis.GetAsync()
    let db = pool.Connection.GetDatabase()

    let! keyExists = db.KeyExistsAsync key
    if not keyExists then
        return None
    else

    let json = db.JSON()
    
    let! result = json.GetAsync<'T>(key)
    return 
        match box result with
        | null -> None
        | value -> Some (unbox value)
}

let createPersonIndex (redis:IConnectionMultiplexerPool) =
    task {
        let! pool = redis.GetAsync()
        let db = pool.Connection.GetDatabase()
        let ft = db.FT()
        try
            ft.Create("PessoaIndex", FTCreateParams().On(IndexDataType.JSON).Prefix("pessoa:"),
                Schema()
                    .AddTextField(FieldName("$.apelido","apelido"))
                    .AddTextField(FieldName("$.nome","nome"))
                    .AddTextField(FieldName("$.stack","stack"))
            ) |> ignore
        with exp ->
            //TODO real logging
            Console.Out.WriteLine (exp.Message)
}

let addPerson (redis:IConnectionMultiplexerPool) (value:Domain.Pessoa) = task {
    let! pool = redis.GetAsync()
    let db = pool.Connection.GetDatabase()
    let json = db.JSON()
    return! json.SetAsync($"pessoa:{value.id}", "$", value)
}
    
let searchPerson (redis:IConnectionMultiplexerPool) query = task {
    let! pool = redis.GetAsync()
    let db = pool.Connection.GetDatabase()
    let ft = db.FT()
    let! result = ft.SearchAsync("PessoaIndex", Query(query).Dialect(3).Limit(0, 50))
    
    return
        result.ToJson()
        |> Seq.map (fun json -> JsonSerializer.Deserialize<Domain.Pessoa[]>(json, Domain.JsonOptions))
        |> Seq.concat
}
    
type IPessoaCache =
    abstract Add : Domain.Pessoa -> bool
    abstract Get : Guid -> Domain.Pessoa option
    abstract GetByApelido : string -> Domain.Pessoa option
    abstract Search : string -> Domain.Pessoa seq
    abstract CreateIndex : unit -> unit
    
type PessoaCache(redis:IConnectionMultiplexerPool) =
    interface IPessoaCache with
        member this.Add (value:Domain.Pessoa) = 
            addPerson redis value
            |> Async.AwaitTask
            |> Async.RunSynchronously
        member this.Get(id:Guid):Domain.Pessoa option = 
            getJson redis $"pessoa:{id}"
            |> Async.AwaitTask
            |> Async.RunSynchronously
        member this.GetByApelido(apelido:string) =
            task {
                let! result = searchPerson redis $"@apelido:({apelido})"
                return result |> Seq.tryHead
            }
            |> Async.AwaitTask
            |> Async.RunSynchronously
        member this.Search(term:string) = 
            searchPerson redis ("%" + term + "%")
            |> Async.AwaitTask
            |> Async.RunSynchronously
        member this.CreateIndex() =
            createPersonIndex redis
            |> Async.AwaitTask
            |> Async.RunSynchronously