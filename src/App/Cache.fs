module App.Cache

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open StackExchange.Redis.MultiplexerPool

module PessoaCache =
    open System.Text.Json
    
    let getJson (redis:IConnectionMultiplexerPool) (key:string) : Task<'T option> = task {
        let! pool = redis.GetAsync()
        let db = pool.Connection.GetDatabase()

        let! keyExists = db.KeyExistsAsync key
        if not keyExists then
            return None
        else
        let! result = db.StringGetAsync(key)
        return 
            match box result with
            | null -> None
            | value -> Some (JsonSerializer.Deserialize<'T>(value.ToString(), Domain.JsonOptions))
    }
    let addPerson (redis:IConnectionMultiplexerPool) (value:Domain.Pessoa) = task {
        let! pool = redis.GetAsync()
        let db = pool.Connection.GetDatabase()
        let json = JsonSerializer.Serialize(value, Domain.JsonOptions)
        return! db.StringSetAsync($"pessoa:{value.id}", json)
    }
            
type IPessoaCache =
    abstract Add : Domain.Pessoa -> unit
    abstract Get : Guid -> Domain.Pessoa option
    
type PessoaCache(logger:ILogger<PessoaCache>, redis:IConnectionMultiplexerPool) =
    
    let tryAddPessoa pessoa =  async {
        let! result = PessoaCache.addPerson redis pessoa |> Async.AwaitTask |> Async.Catch
        match result with
        | Choice1Of2 success ->
            if not success then
                logger.LogError("Erro ao adicionar pessoa {0}", pessoa.id)
                return Error "Erro ao adicionar pessoa"
            else
                return Ok pessoa
        | Choice2Of2 exp  ->
            logger.LogError(exp, "Erro ao adicionar pessoa {0}", pessoa.id)
            return Error "Erro ao adicionar pessoa"
    }
    
    let agent = MailboxProcessor<Domain.Pessoa>.Start(fun inbox ->
        let rec loop () = async {
            let! pessoa = inbox.Receive()
            let! pessoaAdded = tryAddPessoa pessoa
            return! loop()
        }
        loop ()
    )
    
    interface IPessoaCache with
        member this.Add (value:Domain.Pessoa) = agent.Post value
        member this.Get(id:Guid):Domain.Pessoa option = 
            PessoaCache.getJson redis $"pessoa:{id}"
            |> Async.AwaitTask
            |> Async.RunSynchronously


module ApelidoCache =
    let subscribe (redis:IConnectionMultiplexerPool) (channel:string) callback = task {
        let! pool = redis.GetAsync()
        let sub = pool.Connection.GetSubscriber()
        let action = fun _ message -> callback (message |> string)
        let! channel = sub.SubscribeAsync(channel, action)
        return sub
    }
     
type IApelidoCache =
    abstract Add: Domain.Pessoa -> unit
    abstract Test: Domain.Pessoa -> bool
    
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
                return! subscriber.PublishAsync (CHANNEL, pessoa.apelido)
            }
            |> Async.AwaitTask
            |> Async.Ignore
            |> Async.RunSynchronously
        member this.Test pessoa = cache.ContainsKey pessoa.apelido
        
    