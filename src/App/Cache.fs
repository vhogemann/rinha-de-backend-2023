module App.Cache

open System.Text.Json
open StackExchange.Redis

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