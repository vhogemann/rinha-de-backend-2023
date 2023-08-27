module App.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open App.Controller
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
    let createPessoa = Services.inject<NpgsqlConnection, Queue.IPessoaInsertQueue> CreatePessoaHandler
    let getPessoa = Services.inject<NpgsqlConnection> GetPessoaHandler
    let searchPessoas = Services.inject<NpgsqlConnection> SearchPessoasHandler
    let countPessoas = Services.inject<NpgsqlConnection> CountPessoasHandler
    
    webHost args {
        
        add_service (fun services ->
            services
                .AddNpgsqlDataSource(config.GetConnectionString("Default"))
                .AddSingleton<Queue.IPessoaInsertQueue, Queue.PessoaInsertQueue>()    
                // .AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(fun _ ->
                //     let redis = ConnectionMultiplexer.Connect(config.GetConnectionString("Redis"))
                //     Cache.createPersonIndex redis
                //     redis)
        )

        endpoints [
            post "/pessoas" createPessoa
            get "/pessoas/{id}" getPessoa
            get "/pessoas" searchPessoas
            get "/contagem-pessoas" countPessoas
        ]
    }
    0