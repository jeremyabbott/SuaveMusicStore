// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open Suave
open Suave.Http.Successful
open Suave.Web

[<EntryPoint>]
let main argv = 
    startWebServer defaultConfig (OK "Hello World")
    0 // return an integer exit code