module App.Domain
open System
open System.ComponentModel.DataAnnotations
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.Extensions
type [<CLIMutable>] Pessoa = {
    [<Key>]Id: Guid
    Apelido: string
    Nome: string
    Nascimento: DateOnly
    Stack: String[]
    SearchString: string
}
  
type PessoaDbContext(options:DbContextOptions<PessoaDbContext>) =
    inherit DbContext(options)
    [<DefaultValue>]
    val mutable pessoas: DbSet<Pessoa>
    member this.Pessoas with get() = this.pessoas and set(value) = this.pessoas <- value
    override _.OnModelCreating builder =
        builder
            .Entity<Pessoa>()
            .HasIndex("SearchString")
            .HasOperators("text_pattern_ops")
            |> ignore
       
        builder
            .RegisterOptionTypes()
                 
let CreatePessoa (ctx:PessoaDbContext) pessoa = async {
    ctx.Pessoas.Add(pessoa) |> ignore
    let! saved = ctx.SaveChangesAsync() |> Async.AwaitTask
    return
        match saved with
        | 1 -> Some pessoa.Id
        | _ -> None
}
    
let GetPessoa (ctx:PessoaDbContext) (id:Guid) = async {
        let! pessoa = ctx.Pessoas.FindAsync(id).AsTask() |> Async.AwaitTask
        return
            match box pessoa with
            | null -> None
            | _ -> Some pessoa
    }

let SearchPessoa (ctx:PessoaDbContext) (term:string) =
        query {
            for pessoa in ctx.Pessoas do
                where (pessoa.SearchString.Contains(term))
                select pessoa
                take 50
        }
        |> Seq.toList
        
let CountPessoas (ctx:PessoaDbContext) =
    query {
        for pessoa in ctx.Pessoas do
            count
    }