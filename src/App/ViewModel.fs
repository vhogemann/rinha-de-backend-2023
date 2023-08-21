module App.ViewModel

open App.Domain
open System

type CreatePessoa = {
    Apelido: string option
    Nome: string option
    Nascimento: string option
    Stack: String[] option
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
let validateString len (aString:string): bool =
    (String.IsNullOrEmpty aString || aString.Length > len)
    |> not
    
let asPessoa (pessoa:CreatePessoa): Result<Pessoa, string> =
    let validNome = validateString 100
    let validApelido = validateString 32
    let validStack =
        Option.map (Array.map(validateString 32) >> Array.reduce(fun acc x -> acc && x))
        >> Option.defaultValue true
    let nascimento =
        pessoa.Nascimento
        |> Option.bind( fun nascimento ->
            match DateOnly.TryParse(nascimento) with
            | true, value -> Some value
            | _ -> None)
    
    match pessoa.Nome, pessoa.Apelido, pessoa.Stack, nascimento with
    | Some nome, Some apelido, stack, Some nascimento
        when validNome nome && validApelido apelido && validStack stack ->
        let searchString =
            stack
            |> Option.map (Array.reduce (fun acc x -> acc + " " + x))
            |> Option.defaultValue null
        let result:Pessoa = {
            Id = Guid.NewGuid()
            Apelido = apelido
            Nome = nome
            Nascimento = nascimento
            Stack = (stack |> Option.defaultValue [||])
            StackSearch = searchString 
        }
        Ok result
    | _ ->
        Error "Inválido"