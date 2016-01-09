﻿// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

module SuaveMusicStore.App

open System
open Suave
open Suave.Http
open Suave.Types
open Suave.Http.Applicatives
open Suave.Http.RequestErrors
open Suave.Http.Successful
open Suave.Web
open Suave.Form
open Suave.Model.Binding
open Suave.State.CookieStateStore

let passHash (pass: string) =
    use sha = Security.Cryptography.SHA256.Create()
    Text.Encoding.UTF8.GetBytes(pass)
    |> sha.ComputeHash
    |> Array.map (fun b -> b.ToString("x2"))
    |> String.concat ""

type UserLoggedOnSession = {
    Username : string
    Role : string
}

type Session = 
    | NoSession
    | UserLoggedOn of UserLoggedOnSession

let session f =
    statefulForSession
    >>= context (fun x ->
        match x |> HttpContext.state with
        | None -> f NoSession
        | Some state ->
            match state.get "username", state.get "role" with
            | Some username, Some role -> f (UserLoggedOn {Username = username; Role = role})
            | _ -> f NoSession)

let sessionStore setF = context (fun x ->
    match HttpContext.state x with
    | Some state -> setF state
    | None -> never)

let returnPathOrHome = 
    request (fun x -> 
        let path = 
            match (x.queryParam "returnPath") with
            | Choice1Of2 path -> path
            | _ -> Path.home
        Redirection.FOUND path)

let html container =
    OK (View.index container)
    >>= Writers.setMimeType "text/html; charset=utf-8"

let bindToForm form handler =
    bindReq (bindForm form) handler BAD_REQUEST

[<EntryPoint>]
let main argv =
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

    let manage = warbler (fun _ ->
        Db.getContext()
        |> Db.getAlbumsDetails
        |> View.manage
        |> html)

    let deleteAlbum id =
        let ctx = Db.getContext()
        match Db.getAlbum id (ctx) with
        | Some album ->
            choose [
                GET >>= warbler(fun _ ->
                    html (View.deleteAlbum album.Title))
                POST >>= warbler(fun _ ->
                    Db.deleteAlbum album ctx
                    Redirection.FOUND Path.Admin.manage)
            ]
        | None -> never

    let createAlbum =
        let ctx = Db.getContext()
        choose [
            GET >>= warbler (fun _ -> 
                let genres =
                    Db.getGenres ctx
                    |> List.map (fun g -> decimal g.GenreId, g.Name)
                let artists =
                    Db.getArtists ctx
                    |> List.map (fun a -> decimal a.ArtistId, a.Name)
                html (View.createAlbum genres artists))
            POST >>= bindToForm Form.album (fun form ->
                Db.createAlbum (int form.ArtistId, int form.GenreId, form.Price, form.Title) ctx
                Redirection.FOUND Path.Admin.manage)
        ]

    let editAlbum id =
        let ctx = Db.getContext()
        match Db.getAlbum id ctx with
        | Some album ->
            choose [
                GET >>= warbler (fun _ ->
                    let genres = 
                        Db.getGenres ctx 
                        |> List.map (fun g -> decimal g.GenreId, g.Name)
                    let artists = 
                        Db.getArtists ctx
                        |> List.map (fun g -> decimal g.ArtistId, g.Name)
                    html (View.editAlbum album genres artists))
                POST >>= bindToForm Form.album (fun form ->
                    Db.updateAlbum album (int form.ArtistId, int form.GenreId, form.Price, form.Title) ctx
                    Redirection.FOUND Path.Admin.manage)
            ]
        | None -> 
            never

    let logon =
        choose [
            GET >>= (View.logon "" |> html)
            POST >>= bindToForm Form.logon (fun form ->
                let ctx = Db.getContext()
                let (Password password) = form.Password
                match Db.validateUser(form.Username, passHash password) ctx with
                | Some user ->
                        Auth.authenticated Cookie.CookieLife.Session false 
                        >>= session (fun _ -> succeed)
                        >>= sessionStore (fun store ->
                            store.set "username" user.UserName
                            >>= store.set "role" user.Role)
                        >>= returnPathOrHome
                | _ ->
                    View.logon "Username or password is invalid." |> html
            )
        ]

    let webPart =
        choose [
            path Path.home >>= html View.home
            path Path.Store.overview >>= overview
            path Path.Store.browse >>= browse
            pathScan Path.Store.details details
            path Path.Admin.manage >>= manage
            pathScan Path.Admin.deleteAlbum deleteAlbum
            path Path.Admin.createAlbum >>= createAlbum
            pathScan Path.Admin.editAlbum editAlbum
            path Path.Account.logon >>= logon
            pathRegex "(.*)\.(css|png|gif)" >>= Files.browseHome
            html View.notFound
        ]

    startWebServer defaultConfig webPart
    0 // return an integer exit code