module App.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open App.Controller
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Npgsql

[<EntryPoint>]
let main args =
    
    let config = 
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .Build() :> IConfiguration
    
    let createPessoa = Services.inject<NpgsqlConnection> CreatePessoaHandler
    let getPessoa = Services.inject<NpgsqlConnection> GetPessoaHandler
    let searchPessoas = Services.inject<NpgsqlConnection> SearchPessoasHandler
    let countPessoas = Services.inject<NpgsqlConnection> CountPessoasHandler
    
    webHost args {
        
        add_service (fun services ->
            services
                .AddNpgsqlDataSource(config.GetConnectionString("Default"))
        )

        endpoints [
            post "/pessoas" createPessoa
            get "/pessoas/{id}" getPessoa
            get "/pessoas" searchPessoas
            get "/contagem-pessoas" countPessoas
        ]
    }
    0