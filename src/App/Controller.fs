module App.Controller

open App.Domain
open Falco
open System

type CreatePessoa = {
    Apelido: string
    Nome: string
    Nascimento: DateOnly
    Stack: String[]
} with
    member this.AsPessoa =
        let searchString =
            this.Stack
            |> Array.reduce (fun acc x -> acc + " " + x)
            |> fun x -> this.Apelido + " " + this.Nome + " " + x
        {
            Id = Guid.NewGuid()
            Apelido = this.Apelido
            Nome = this.Nome
            Nascimento = this.Nascimento
            Stack = this.Stack
            SearchString = searchString
        }

type ViewPessoa = {
    Id: Guid
    Apelido: string
    Nome: string
    Nascimento: DateOnly
    Stack: String[]
} with
    static member FromPessoa (pessoa:Pessoa) =
        {
            Id = pessoa.Id
            Apelido = pessoa.Apelido
            Nome = pessoa.Nome
            Nascimento = pessoa.Nascimento
            Stack = pessoa.Stack
        }
 
let CreatePessoaHandler (dbCtx:PessoaDbContext) : HttpHandler =
    let handleCreatePessoa (createPessoa:CreatePessoa) : HttpHandler =
        let person = createPessoa.AsPessoa
        let result = CreatePessoa dbCtx person |> Async.RunSynchronously
        match result with
        | Some uid -> Response.withStatusCode 204 >> Response.ofPlainText (uid.ToString())
        | None -> Response.withStatusCode 500 >> Response.ofPlainText "Error saving pessoa to the database"
    Request.mapJson handleCreatePessoa
    
let GetPessoaHandler (dbCtx:PessoaDbContext) : HttpHandler = fun ctx ->
    let r = Request.getRoute ctx
    let id = r.GetGuid "id"
    let maybePessoa = GetPessoa dbCtx id |> Async.RunSynchronously
    match maybePessoa with
    | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson (ViewPessoa.FromPessoa pessoa)) ctx
    | None ->  (Response.withStatusCode 404 >> Response.ofPlainText "Pessoa not found") ctx
    
let SearchPessoasHandler (dbCtx:PessoaDbContext) : HttpHandler = fun ctx ->
    let r = Request.getQuery ctx
    let query = r.GetString "t"
    let pessoas =
        SearchPessoa dbCtx query
        |> Array.ofSeq
    (Response.withStatusCode 200 >> Response.ofJson pessoas) ctx
    
let CountPessoasHandler (dbCtx:PessoaDbContext) : HttpHandler = fun ctx ->
    let count = CountPessoas dbCtx
    (Response.withStatusCode 200 >> Response.ofPlainText (count.ToString())) ctx