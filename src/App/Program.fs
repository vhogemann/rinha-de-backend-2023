module App.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Npgsql
open StackExchange.Redis

[<EntryPoint>]
let main args =
    
    let config = 
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .Build() :> IConfiguration
    let createPessoa =
        Services.inject<NpgsqlConnection, Queue.IPessoaInsertQueue, Cache.IPessoaCache> Controller.CreatePessoaHandler
    let getPessoa =
        Services.inject<NpgsqlConnection> Controller.GetPessoaHandler
    let searchPessoas =
        Services.inject<NpgsqlConnection> Controller.SearchPessoasHandler
    let countPessoas =
        Services.inject<NpgsqlConnection> Controller.CountPessoasHandler
    
    webHost args {
        
        add_service (fun services ->
            services
                .AddNpgsqlDataSource(config.GetConnectionString("Default"))
                .AddSingleton<Queue.IPessoaInsertQueue, Queue.PessoaInsertQueue>()    
                .AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(fun _ ->
                    let redis = ConnectionMultiplexer.Connect(config.GetConnectionString("Redis"))
                    Cache.createPersonIndex redis
                    redis
                )
                .AddSingleton<Cache.IPessoaCache, Cache.PessoaCache>()
        )

        endpoints [
            post "/pessoas" createPessoa
            get "/pessoas/{id}" getPessoa
            get "/pessoas" searchPessoas
            get "/contagem-pessoas" countPessoas
        ]
    }
    0