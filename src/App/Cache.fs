module App.Cache

open System
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.Caching.Memory
open Microsoft.Extensions.Logging
open StackExchange.Redis.MultiplexerPool
module PessoaCache =
    let subscribe (redis:IConnectionMultiplexerPool) (channel:string) callback = task {
        let! pool = redis.GetAsync()
        let sub = pool.Connection.GetSubscriber()
        let action = fun _ message -> callback (message |> string)
        do! sub.SubscribeAsync(channel, action)
        return sub
    }
     
type IPessoaCache =
    abstract Add: Domain.Pessoa -> Task<unit>
    abstract Exists: Domain.Pessoa -> bool
    abstract Get: Guid -> Domain.Pessoa option
    
type PessoaCache(logger:ILogger<PessoaCache>,redis:IConnectionMultiplexerPool, cache:IMemoryCache) =
    let PESSOA = "pessoa"

    let subscribeAsync =
        PessoaCache.subscribe redis PESSOA (fun message ->
            let pessoa = JsonSerializer.Deserialize<Domain.Pessoa>(message, Domain.JsonOptions)
            cache.Set(pessoa.apelido, true)|> ignore
            cache.Set(pessoa.id, pessoa)|> ignore)

    interface IPessoaCache with
        member this.Add pessoa =
            cache.Set(pessoa.apelido, true) |> ignore
            cache.Set(pessoa.id, pessoa) |> ignore
            task {
                let! subscriber = subscribeAsync
                let json = JsonSerializer.Serialize(pessoa, Domain.JsonOptions)
                let! _ =  subscriber.PublishAsync (PESSOA, json)
                return ()
            }
        member this.Exists pessoa = cache.Get(pessoa.apelido) <> null
        member this.Get id =
            match cache.Get(id) with
            | null -> None
            | :? Domain.Pessoa as pessoa -> Some pessoa
            | _ -> None