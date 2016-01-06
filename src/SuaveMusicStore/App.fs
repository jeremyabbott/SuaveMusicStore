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

    let details id =
        match Db.getAlbumDetails id (Db.getContext()) with
        | Some album -> html (View.details album)
        | None -> never

    let webPart =
        choose [
            path Path.home >>= html View.home
            path Path.Store.overview >>= overview
            path Path.Store.browse >>= browse
            pathScan Path.Store.details details
            pathRegex "(.*)\.(css|png|gif)" >>= Files.browseHome
            html View.notFound
        ]

    startWebServer defaultConfig webPart
    0 // return an integer exit code