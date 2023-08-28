module App.Cache

open System
open System.Text.Json
open NRedisStack.Search
open NRedisStack.Search.Literals.Enums
open StackExchange.Redis
open NRedisStack.RedisStackCommands

let addJson (redis:IConnectionMultiplexer) (key:string) value =
    let db = redis.GetDatabase()
    let json = db.JSON()
    json.Set(key, "$", value ) |> ignore

let getJson (redis:IConnectionMultiplexer) (key:string) : 'T option=
    let db = redis.GetDatabase()

    if db.KeyExists key |> not then
        None
    else

    let json = db.JSON()
    match json.Get<'T> (key,"$") |> box with
    | null -> None
    | value -> Some (unbox value)

let createPersonIndex (redis:IConnectionMultiplexer) =
    let db = redis.GetDatabase()
    let ft = db.FT()
    ft.Create("PessoaIndex", FTCreateParams().On(IndexDataType.JSON).Prefix("pessoa:"),
            Schema()
                .AddTagField(FieldName("$.id","id" ))
                .AddTagField(FieldName("$.apelido","apelido"))
                .AddTagField(FieldName("$.nome","nome"))
                .AddTagField(FieldName("$.stack[*]","stack"))
    ) |> ignore

let addPerson (redis:IConnectionMultiplexer) (value:Domain.Pessoa) =
    let db = redis.GetDatabase()
    let json = db.JSON()
    json.Set($"pessoa:{value.id}", "$", value) |> ignore
    
let searchPerson (redis:IConnectionMultiplexer) query =
    let db = redis.GetDatabase()
    let ft = db.FT()
    let result = ft.Search("PessoaIndex", Query(query).Dialect(3).Limit(0, 50))
    result.ToJson()
    |> Seq.map JsonSerializer.Deserialize<Domain.Pessoa>
    
type IPessoaCache =
    abstract Add : Domain.Pessoa -> unit
    abstract Get : Guid -> Domain.Pessoa option
    abstract GetByApelido : string -> Domain.Pessoa option
    abstract Search : string -> Domain.Pessoa seq
    
type PessoaCache(redis:IConnectionMultiplexer) =
    interface IPessoaCache with
        member this.Add (value:Domain.Pessoa) = 
            addPerson redis value
        member this.Get(id:Guid):Domain.Pessoa option = 
            getJson redis $"pessoa:{id}"
        member this.GetByApelido(apelido:string) = 
            searchPerson redis $"@apelido:({apelido})" |> Seq.tryHead
        member this.Search(term:string) = 
            searchPerson redis term