module App.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open App.Domain
open App.Controller
open Microsoft.Extensions.Configuration

[<EntryPoint>]
let main args =
    let config =
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build()
    let dbCtx = new PessoaDbContext(config.GetConnectionString("Default"))
    dbCtx.Database.EnsureCreated() |> ignore
    
    let createPessoa = CreatePessoaHandler dbCtx
    let getPessoa = GetPessoaHandler dbCtx
    let searchPessoas = SearchPessoasHandler dbCtx
    let countPessoas = CountPessoasHandler dbCtx
    
    webHost args {
        
        
        endpoints [
            post "/pessoas" createPessoa
            get "/pessoas/{id}" getPessoa
            get "/pessoas" searchPessoas
            get "/contagem-pessoas" countPessoas
        ]
    }
    0