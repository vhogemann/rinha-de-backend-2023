module App.Domain
open System
open System.ComponentModel.DataAnnotations
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.Extensions
open System.Linq
open NpgsqlTypes
type [<CLIMutable>] Pessoa = {
    [<Key>]Id: Guid
    Apelido: string
    Nome: string
    Nascimento: DateOnly
    Stack: String[]
    StackSearch: string
}
  
type PessoaDbContext(options:DbContextOptions<PessoaDbContext>) =
    inherit DbContext(options)
    [<DefaultValue>]
    val mutable pessoas: DbSet<Pessoa>
    member this.Pessoas with get() = this.pessoas and set(value) = this.pessoas <- value
    override _.OnModelCreating builder =
        builder
            .Entity<Pessoa>()
            .HasIndex("Apelido", "Nome", "StackSearch")
            .HasMethod("GIN")
            .IsTsVectorExpressionIndex("english")
            |> ignore
            
        builder
            .RegisterOptionTypes()
            
                 
let CreatePessoa (ctx:PessoaDbContext) pessoa =
    ctx.Pessoas.Add(pessoa) |> ignore
    ctx.SaveChanges()
    
let GetPessoa (ctx:PessoaDbContext) (id:Guid) =
    let result = ctx.Pessoas.Find(id)
    match box result with
    | null -> None
    | _ -> Some result

let ExistsPessoaByApelido (ctx:PessoaDbContext) (apelido:string) =
    query {
        for pessoa in ctx.Pessoas do
        where (pessoa.Apelido = apelido)
        count
    } > 1

let SearchPessoa (ctx:PessoaDbContext) (term:string) =
    ctx.Pessoas
        .Where(fun p -> EF.Functions.ToTsVector("english",p.Apelido + " " + p.Nome + " " + p.StackSearch).Matches($"{term}:*"))
        .ToList()
        
let CountPessoas (ctx:PessoaDbContext) =
    query {
        for pessoa in ctx.Pessoas do
            count
    }
