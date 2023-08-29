module App.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Npgsql
open StackExchange.Redis.MultiplexerPool

[<EntryPoint>]
let main args =
    
    let config = 
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .Build() :> IConfiguration
    let createPessoa =
        Services.inject<NpgsqlConnection, Queue.IPessoaInsertQueue, Cache.IPessoaCache> Controller.CreatePessoaHandler
    let getPessoa =
        Services.inject<NpgsqlConnection, Cache.IPessoaCache> Controller.GetPessoaHandler
    let searchPessoas =
        Services.inject<NpgsqlConnection, Cache.IPessoaCache> Controller.SearchPessoasHandler
    let countPessoas =
        Services.inject<NpgsqlConnection> Controller.CountPessoasHandler
    
    webHost args {
        
        add_service (fun services ->
            services
                .AddNpgsqlDataSource(config.GetConnectionString("Default"))
                .AddSingleton<Queue.IPessoaInsertQueue, Queue.PessoaInsertQueue>()
                .AddSingleton<IConnectionMultiplexerPool>(fun _ ->
                    ConnectionMultiplexerPoolFactory.Create(
                        50,
                        config.GetConnectionString("Redis")))
                .AddSingleton<Cache.IPessoaCache>( fun ctx ->
                    let pool = ctx.GetService<IConnectionMultiplexerPool>()
                    let cache = Cache.PessoaCache(pool) :> Cache.IPessoaCache
                    cache.CreateIndex()
                    cache
                )
        )

        endpoints [
            post "/pessoas" createPessoa
            get "/pessoas/{id}" getPessoa
            get "/pessoas" searchPessoas
            get "/contagem-pessoas" countPessoas
        ]
    }
    0