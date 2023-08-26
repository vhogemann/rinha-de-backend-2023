module App.ViewModel

open App.Domain
open System

type CreatePessoa = {
    Apelido: string option
    Nome: string option
    Nascimento: string option
    Stack: String[] option
}

let validateString len (aString:string): bool =
    (String.IsNullOrEmpty aString || aString.Length > len)
    |> not
    
let asPessoa (pessoa:CreatePessoa): Result<Pessoa, int*string> =
    let validNome = validateString 100
    let validApelido = validateString 32
    let validStack =
        Option.map (fun arr ->
            if Array.isEmpty arr then true else 
            arr |> (Array.map(validateString 32) >> Array.reduce(fun acc x -> acc && x)))
        >> Option.defaultValue true
    let nascimento =
        pessoa.Nascimento
        |> Option.bind( fun nascimento ->
            match DateOnly.TryParse(nascimento) with
            | true, value -> Some (value.ToDateTime(TimeOnly.MinValue).ToUniversalTime())
            | _ ->
                System.Console.WriteLine("Data invalida")
                None)
    
    match pessoa.Nome, pessoa.Apelido, pessoa.Stack, nascimento with
    | Some nome, Some apelido, stack, Some nascimento
        when validNome nome && validApelido apelido && validStack stack ->
        let result:Pessoa = {
            Id = Guid.NewGuid()
            Apelido = apelido
            Nome = nome
            Nascimento = nascimento
            Stack = (stack |> Option.defaultValue [||])
        }
        Ok result
    | _ ->
        Error (400, "Json invalido")
