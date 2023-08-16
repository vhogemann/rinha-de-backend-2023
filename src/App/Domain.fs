module App.Domain
open System
open System.ComponentModel.DataAnnotations
open Microsoft.EntityFrameworkCore

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
    override _.OnConfiguring(optionsBuilder: DbContextOptionsBuilder) =
        optionsBuilder
            .UseNpgsql(connectionString) |> ignore
        
let db = new DBContext("Host=localhost;Database=postgres;Username=postgres;Password=postgres")

let SavePessoa pessoa =
    db.Pessoas.Add(pessoa) |> ignore
    db.SaveChangesAsync() |> Async.AwaitTask
    
let GetPessoa (uid:Guid) = async {
    let! result =
        db.Pessoas.FindAsync(uid).AsTask()
        |> Async.AwaitTask
    return
        match box result with
        | null -> None
        | _ -> Some result
}