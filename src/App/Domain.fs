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
    Stack: String list
}

type DBContext(connectionString:string) =
    inherit DbContext()
    [<DefaultValue>]
    val mutable pessoas: DbSet<Pessoa>
    member this.Pessoas with get() = this.pessoas and set(value) = this.pessoas <- value
    override _.OnModelCreating builder =
        builder.RegisterOptionTypes()
    override _.OnConfiguring(optionsBuilder: DbContextOptionsBuilder) =
        optionsBuilder
            .UseNpgsql(connectionString) |> ignore
        
let db = new DBContext("Host=localhost;Database=postgres;Username=postgres;Password=postgres")

let SavePessoa pessoa =
    db.Pessoas.Add(pessoa) |> ignore
    db.SaveChangesAsync() |> Async.AwaitTask
    
let GetPessoa (ctx:DBContext) (id:Guid) = async {
        let! pessoa = ctx.Pessoas.FindAsync(id).AsTask() |> Async.AwaitTask
        return
            match box pessoa with
            | null -> None
            | _ -> Some pessoa
    }

let FindPessoas (ctx:DBContext) (termo:string) =
        query {
            for pessoa in ctx.Pessoas do
                where (pessoa.Apelido = apelido)
                select pessoa
        }
        |> Seq.toList