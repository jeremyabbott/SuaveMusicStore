// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

module SuaveMusicStore.App

open Suave
open Suave.Http
open Suave.Types
open Suave.Http.Applicatives
open Suave.Http.RequestErrors
open Suave.Http.Successful
open Suave.Web

[<EntryPoint>]
let main argv =
    let html container =
        OK (View.index container)
        >>= Writers.setMimeType "text/html; charset=utf-8"

    let browse =
        request (fun r ->
            match r.queryParam Path.Store.browseKey with
            | Choice1Of2 genre ->
                Db.getContext()
                |> Db.getAlbumsForGenre genre
                |> View.browse genre
                |> html
            | Choice2Of2 msg -> BAD_REQUEST msg)

    let overview = warbler(fun _ ->
        Db.getContext() 
        |> Db.getGenres 
        |> List.map (fun g -> g.Name) 
        |> View.store 
        |> html)

    let webPart =
        choose [
            path Path.home >>= html View.home
            path Path.Store.overview >>= overview
           // path Path.Store.overview >>= html (View.store ["Rock"; "Disco"; "Pop"])
            path Path.Store.browse >>= browse
            pathScan Path.Store.details (fun id -> html (View.details id))
            pathRegex "(.*)\.(css|png)" >>= Files.browseHome
        ]

    startWebServer defaultConfig webPart
    0 // return an integer exit code