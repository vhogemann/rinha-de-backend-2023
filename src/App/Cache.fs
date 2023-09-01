module App.Cache

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open StackExchange.Redis.MultiplexerPool
module ApelidoCache =
    let subscribe (redis:IConnectionMultiplexerPool) (channel:string) callback = task {
        let! pool = redis.GetAsync()
        let sub = pool.Connection.GetSubscriber()
        let action = fun _ message -> callback (message |> string)
        let! channel = sub.SubscribeAsync(channel, action)
        return sub
    }
     
type IApelidoCache =
    abstract Add: Domain.Pessoa -> Task<unit>
    abstract Exists: Domain.Pessoa -> bool
    
type ApelidoCache(logger:ILogger<ApelidoCache>,redis:IConnectionMultiplexerPool) =
    let CHANNEL = "apelido"
    let cache = ConcurrentDictionary<String,bool>()
   
    let subscribeAsync =
        ApelidoCache.subscribe redis CHANNEL (fun message ->
            logger.LogInformation("Received message - {}", message)
            cache.TryAdd(message, true)|> ignore )
    
    interface IApelidoCache with
        member this.Add pessoa =
            task {
                let! subscriber = subscribeAsync
                let! _ =  subscriber.PublishAsync (CHANNEL, pessoa.apelido)
                return ()
            }
            
        member this.Exists pessoa = cache.ContainsKey pessoa.apelido
        
    