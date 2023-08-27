module App.Cache

open System.Text.Json
open NRedisStack.Search
open NRedisStack.Search.Literals.Enums
open StackExchange.Redis
open NRedisStack.RedisStackCommands

let memoize (redis:ConnectionMultiplexer) f =
    fun c ->
        let cache = redis.GetDatabase()
        let value = cache.StringGet $"%A{c}"
        if value.HasValue then
            value |> JsonSerializer.Deserialize<'T>
        else
            let value = f c 
            cache.StringSet($"%A{c}", value |> JsonSerializer.Serialize ) |> ignore
            value

let add (redis:ConnectionMultiplexer) (key:string) value =
    let db = redis.GetDatabase()
    db.StringSet(key, value |> JsonSerializer.Serialize ) |> ignore

let get (redis:ConnectionMultiplexer) (key:string) =
    let db = redis.GetDatabase()
    let value = db.StringGet(key)
    if value.HasValue then
        value |> JsonSerializer.Deserialize<'T> |> Some
    else
        None
    
let addJson (redis:ConnectionMultiplexer) (key:string) value =
    let db = redis.GetDatabase()
    let json = db.JSON()
    json.Set(key, "$", value |> JsonSerializer.Serialize ) |> ignore

let createPersonIndex (redis:ConnectionMultiplexer) =
    let db = redis.GetDatabase()
    let ft = db.FT()
    ft.Create("PessoaIndex", FTCreateParams().On(IndexDataType.JSON).Prefix("pessoa:"),
              Schema()
                  .AddTagField(FieldName("$.id","id" ))
                  .AddTagField(FieldName("$.apelido","apelido"))
                  .AddTagField(FieldName("$.nome","nome"))
                  .AddTagField(FieldName("$.stack","stack[*]"))
              ) |> ignore

let addPerson (redis:ConnectionMultiplexer) (value:Domain.Pessoa) =
    let db = redis.GetDatabase()
    let json = db.JSON()
    json.Set($"pessoa:{value.Id}", "$", value) |> ignore
    
let searchPerson (redis:ConnectionMultiplexer) query =
    let db = redis.GetDatabase()
    let ft = db.FT()
    let result = ft.Search("PessoaIndex", Query($"@apelido:{{{query}}}").Limit(0, 50))
    result.ToJson()
    |> Seq.map JsonSerializer.Deserialize<Domain.Pessoa>