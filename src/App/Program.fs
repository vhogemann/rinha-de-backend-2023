module App.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Npgsql
open StackExchange.Redis.MultiplexerPool

[<EntryPoint>]
let main args =
    
    let config = 
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .Build() :> IConfiguration
    let createPessoa =
        Services.inject<Queue.IPessoaInsertQueue, Cache.IPessoaCache> Controller.CreatePessoaHandler
    let getPessoa =
        Services.inject<Cache.IPessoaCache> Controller.GetPessoaHandler
    let searchPessoas =
        Services.inject<Domain.IRepository> Controller.SearchPessoasHandler
    let countPessoas =
        Services.inject<Domain.IRepository> Controller.CountPessoasHandler
        
    webHost args {

        logging (fun builder -> builder.AddConsole())
        
        add_service (fun services ->
            services
                .AddNpgsqlDataSource(config.GetConnectionString("Default"))
                .AddSingleton<Domain.IRepository, Domain.Repository>()
                .AddSingleton<Queue.IPessoaInsertQueue, Queue.PessoaInsertQueue>()
                .AddSingleton<IConnectionMultiplexerPool>(fun _ ->
                    ConnectionMultiplexerPoolFactory.Create(
                        50,
                        config.GetConnectionString("Redis")))
                .AddSingleton<Cache.IPessoaCache, Cache.PessoaCache>()
                .AddLogging()
                .AddMemoryCache()
        )
       
        endpoints [
            post "/pessoas" createPessoa
            get "/pessoas/{id}" getPessoa
            get "/pessoas" searchPessoas
            get "/contagem-pessoas" countPessoas
        ]
    }
    0