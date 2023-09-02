module App.Cache

open System
open System.Collections.Concurrent
open System.Threading.Tasks
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
    
type PessoaCache(logger:ILogger<PessoaCache>,redis:IConnectionMultiplexerPool) =
    let APELIDO = "apelido"
    let apelidoCache = ConcurrentDictionary<String,bool>()
    let subscribeAsync =
        PessoaCache.subscribe redis APELIDO (fun message ->
            logger.LogInformation("Received message - {}", message)
            apelidoCache.TryAdd(message, true)|> ignore )
    interface IPessoaCache with
        member this.Add pessoa =
            apelidoCache.TryAdd(pessoa.apelido, true) |> ignore
            task {
                let! subscriber = subscribeAsync
                let! _ =  subscriber.PublishAsync (APELIDO, pessoa.apelido)
                return ()
            }
        member this.Exists pessoa = apelidoCache.ContainsKey pessoa.apelido
        