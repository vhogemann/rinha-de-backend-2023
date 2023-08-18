module App.Program

open System
open Falco
open Falco.Routing
open Falco.HostBuilder
open App.Domain
open App.Controller
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection

let ensureDatabaseCreated (builder:IApplicationBuilder) =
    use serviceScope = builder.ApplicationServices.CreateScope()
    let db = serviceScope.ServiceProvider.GetService<PessoaDbContext>()
    db.Database.EnsureCreated() |> ignore
    builder

[<EntryPoint>]
let main args =
    
    let createPessoa = Services.inject<PessoaDbContext> CreatePessoaHandler
    let getPessoa = Services.inject<PessoaDbContext> GetPessoaHandler
    let searchPessoas = Services.inject<PessoaDbContext> SearchPessoasHandler
    let countPessoas = Services.inject<PessoaDbContext> CountPessoasHandler
    
    let config = 
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .Build() :> IConfiguration

    webHost args {
        
        add_service (fun services ->
            services.AddDbContext<PessoaDbContext>(fun builder ->
                builder.UseNpgsql(config.GetConnectionString("Default")) |> ignore
            )
        )

        use_if FalcoExtensions.IsDevelopment ensureDatabaseCreated

        endpoints [
            post "/pessoas" createPessoa
            get "/pessoas/{id}" getPessoa
            get "/pessoas" searchPessoas
            get "/contagem-pessoas" countPessoas
        ]
    }
    0