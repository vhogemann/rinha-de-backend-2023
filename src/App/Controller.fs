module App.Controller

open App.Domain
open Falco
open System
let validateString len (aString:string): bool =
    String.IsNullOrEmpty aString || aString.Length > len
    |> not
type CreatePessoa = {
    Apelido: string option
    Nome: string option
    Nascimento: string option
    Stack: String[] option
} with
    member this.AsPessoa() =
        match this.Apelido, this.Nome, this.Nascimento with
        | Some apelido, Some nome, Some nascimento when
            (validateString 32 apelido) && (validateString 100 nome) && (validateString 10 nascimento)->
            let couldParse, nascimento = DateOnly.TryParse nascimento
         
            let validStack =
                this.Stack
                |> Option.map(Array.map(validateString 32) >> Array.reduce(fun acc x -> acc && x))
                |> Option.defaultValue true
            
            if not couldParse || not validStack then
                Error "Inválido"
            else
            
            let searchString =
                this.Stack
                |> Option.map (Array.reduce (fun acc x -> acc + " " + x))
                |> Option.map (fun x -> apelido + " " + nome + " " + x)
                |> Option.defaultValue (apelido + " " + nome)
            {
                Id = Guid.NewGuid()
                Apelido = apelido
                Nome = nome
                Nascimento = nascimento
                Stack = this.Stack |> Option.defaultValue [||]
                SearchString = searchString
            }
            |> Ok
        | _ ->
            Error "Inválido"

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

let CreatePessoaHandler (dbCtx:PessoaDbContext) =
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec loop () = async {
            let! pessoa = inbox.Receive()
            do! CreatePessoa dbCtx pessoa
            return! loop()
        }
        loop()
    )
    let handleCreatePessoa (createPessoa:CreatePessoa) : HttpHandler =
        let person = createPessoa.AsPessoa()
        
        match person with
        | Error message ->
            (Response.withStatusCode 400 >> Response.ofPlainText message)
        | Ok person ->
            
        agent.Post(person)
        (Response.withStatusCode 201
         >> Response.withHeaders [ ("Location", $"/pessoas/{person.Id}") ]
         >> Response.ofJson (ViewPessoa.FromPessoa person))
        
    Request.mapJson handleCreatePessoa
    
let GetPessoaHandler (dbCtx:PessoaDbContext) : HttpHandler = fun ctx ->
    let r = Request.getRoute ctx
    let id = r.GetGuid "id"
    let maybePessoa = GetPessoa dbCtx id |> Async.RunSynchronously
    match maybePessoa with
    | Some pessoa -> (Response.withStatusCode 200 >> Response.ofJson (ViewPessoa.FromPessoa pessoa)) ctx
    | None ->  (Response.withStatusCode 404 >> Response.ofEmpty) ctx
    
let SearchPessoasHandler (dbCtx:PessoaDbContext) : HttpHandler = fun ctx ->
    let r = Request.getQuery ctx
    let query = r.GetString "t"
    match query with
    | null 
    | "" -> (Response.withStatusCode 400 >> Response.ofEmpty) ctx
    | query ->
    let pessoas =
        SearchPessoa dbCtx query
        |> Seq.map ViewPessoa.FromPessoa
        |> Array.ofSeq
    (Response.withStatusCode 200 >> Response.ofJson pessoas) ctx
    
let CountPessoasHandler (dbCtx:PessoaDbContext) : HttpHandler = fun ctx ->
    let count = CountPessoas dbCtx
    (Response.withStatusCode 200 >> Response.ofPlainText (count.ToString())) ctx