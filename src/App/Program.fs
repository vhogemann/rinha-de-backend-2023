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

    webHost args {

        logging (fun builder -> builder.AddConsole())
        
        add_service (fun services ->
            services
                .AddLogging()
                .AddMemoryCache()
                .AddNpgsqlDataSource(config.GetConnectionString("Default"))
                .AddSingleton<Queue.IPessoaInsertQueue, Queue.PessoaInsertQueue>()
                .AddScoped<Domain.IRepository, Domain.Repository>()
                .AddScoped<IConnectionMultiplexerPool>(fun _ ->
                    ConnectionMultiplexerPoolFactory.Create(
                        100,
                        config.GetConnectionString("Redis")))
                .AddSingleton<Cache.IPessoaCache, Cache.PessoaCache>()
        )
       
        endpoints [
            post "/pessoas" Controller.CreatePessoa.handler
            get "/pessoas/{id}" Controller.GetPessoa.handler
            get "/pessoas" Controller.SearchPessoa.handler
            get "/contagem-pessoas" Controller.CountPessoas.handler
        ]
    }
    0