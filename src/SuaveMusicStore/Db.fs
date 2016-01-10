module SuaveMusicStore.Db

open System
open FSharp.Data.Sql

type Sql = 
    SqlDataProvider< 
        "Data source=localhost;Initial catalog=SuaveMusicStore;Trusted_Connection=True;MultipleActiveResultSets=true", 
        DatabaseVendor=Common.DatabaseProviderTypes.MSSQLSERVER >

type DbContext = Sql.dataContext
type Album = DbContext.``[dbo].[Albums]Entity``
type Genre = DbContext.``[dbo].[Genres]Entity``
type AlbumDetails = DbContext.``[dbo].[AlbumDetails]Entity``
type Artist = DbContext.``[dbo].[Artists]Entity``
type User = DbContext.``[dbo].[Users]Entity``
type CartDetails = DbContext.``[dbo].[CartDetails]Entity``
type Cart = DbContext.``[dbo].[Carts]Entity``

let firstOrNone s = s |> Seq.tryFind (fun _ -> true)

let getGenres (ctx : DbContext) : Genre list = 
    ctx.``[dbo].[Genres]`` |> Seq.toList

let getAlbumsForGenre genreName (ctx : DbContext) : Album list = 
    query { 
        for album in ctx.``[dbo].[Albums]`` do
            join genre in ctx.``[dbo].[Genres]`` on (album.GenreId = genre.GenreId)
            where (genre.Name = genreName)
            select album
    }
    |> Seq.toList

let getAlbumDetails id (ctx : DbContext) : AlbumDetails option = 
    query { 
        for album in ctx.``[dbo].[AlbumDetails]`` do
            where (album.AlbumId = id)
            select album
    } |> firstOrNone

let getContext() = Sql.GetDataContext()

let getAlbumsDetails (ctx : DbContext) : AlbumDetails list =
    ctx.``[dbo].[AlbumDetails]`` |> Seq.toList

let getAlbum id (ctx: DbContext) : Album option =
    query {
        for album in ctx.``[dbo].[Albums]`` do
            where (album.AlbumId = id)
            select album
    } |> firstOrNone

let deleteAlbum (album : Album) (ctx : DbContext) =
    album.Delete()
    ctx.SubmitUpdates()

let getArtists (ctx : DbContext) : Artist list =
    ctx.``[dbo].[Artists]`` |> Seq.toList

let createAlbum (artistId, genreId, price, title) (ctx : DbContext) =
    ctx.``[dbo].[Albums]``.Create(artistId, genreId, price, title) |> ignore
    ctx.SubmitUpdates()

let updateAlbum (album : Album) (artistId, genreId, price, title) (ctx : DbContext) =
    album.ArtistId <- artistId
    album.GenreId <- genreId
    album.Price <- price
    album.Title <- title
    ctx.SubmitUpdates()

let validateUser (username, password) (ctx : DbContext) : User option =
    query {
        for user in ctx.``[dbo].[Users]`` do
            where (user.UserName = username && user.Password = password)
            select user
    } |> firstOrNone

let getCart cartId albumId (ctx : DbContext) : Cart option =
    query {
        for cart in ctx.``[dbo].[Carts]`` do
            where (cart.CartId = cartId && cart.AlbumId = albumId)
            select cart
    } |> firstOrNone

let addToCart cartId albumId (ctx : DbContext)  =
    match getCart cartId albumId ctx with
    | Some cart ->
        cart.Count <- cart.Count + 1
    | None ->
        ctx.``[dbo].[Carts]``.Create(albumId, cartId, 1, DateTime.UtcNow) |> ignore
    ctx.SubmitUpdates()

let getCartsDetails cartId (ctx : DbContext) : CartDetails list =
    query {
        for cart in ctx.``[dbo].[CartDetails]`` do
            where (cart.CartId = cartId)
            select cart
    } |> Seq.toList

let removeFromCart (cart : Cart) albumId (ctx : DbContext) = 
    cart.Count <- cart.Count - 1
    if cart.Count = 0 then cart.Delete()
    ctx.SubmitUpdates()