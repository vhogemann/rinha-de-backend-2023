module Test

open System.Text.Json
open System.Text.Json.Serialization
open App
open Microsoft.EntityFrameworkCore.Metadata.Internal
open NUnit.Framework
open Swensen.Unquote

let options =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    options.AllowTrailingCommas <- true
    options.PropertyNameCaseInsensitive <- true
    options

[<SetUp>]
let Setup () =
    ()

[<Test>]
let ``Test CreatePessoa.AsPessoa`` () =
    let json ="""{"apelido" : "xpto", "nome" : "xpto xpto", "nascimento" : "2000-01-01", "stack": null}"""
    
    let createPessoa = JsonSerializer.Deserialize<ViewModel.CreatePessoa>(json, options)
    
    let pessoa = ViewModel.asPessoa createPessoa
    
    test <@ Result.isOk pessoa @>
    