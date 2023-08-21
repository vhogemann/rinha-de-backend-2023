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
