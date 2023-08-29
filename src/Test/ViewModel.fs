module Test

open System
open System.Text.Json
open System.Text.Json.Serialization
open App
open Microsoft.EntityFrameworkCore.Metadata.Internal
open NUnit.Framework
open Swensen.Unquote

let options =
    Domain.JsonOptions

[<SetUp>]
let Setup () =
    ()

[<Test>]
let ``Test CreatePessoa.AsPessoa`` () =
    let json ="""{"apelido" : "xpto", "nome" : "xpto xpto", "nascimento" : "2000-01-01", "stack": null}"""
   
    let pessoa =
        JsonSerializer.Deserialize<ViewModel.CreatePessoa>(json, options)
        |> ViewModel.asPessoa
    
    test <@ Result.isOk pessoa @>
    
    let json ="""{"apelido" : "xpto", "nome" : "xpto xpto", "nascimento" : "2000-01-01", "stack": []}"""
    
    let pessoa =
        JsonSerializer.Deserialize<ViewModel.CreatePessoa>(json, options)
        |> ViewModel.asPessoa
    
    test <@ Result.isOk pessoa @>
    
[<Test>]
let ``Test deserialize pessoa`` () =
    let json ="""{"id":"c27d8cf3-7d54-4100-b084-9b9db0542c56","apelido":"test","nome":"xpto xpto","nascimento":"2000-01-01T00:00:00Z","stack":["teste"]}"""
    
    let pessoa = JsonSerializer.Deserialize<Domain.Pessoa>(json, options)
    
    test <@ pessoa.id = Guid.Parse("c27d8cf3-7d54-4100-b084-9b9db0542c56") @>