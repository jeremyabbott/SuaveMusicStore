module SuaveMusicStore.Db

open FSharp.Data.Sql

type Sql = 
    SqlDataProvider< 
        "Data source=localhost;Initial catalog=SuaveMusicStore;Trusted_Connection=True;MultipleActiveResultSets=true", 
        DatabaseVendor=Common.DatabaseProviderTypes.MSSQLSERVER >

type DbContext = Sql.dataContext
type Album = DbContext.``[dbo].[Albums]Entity``
type Genre = DbContext.``[dbo].[Genres]Entity``
type AlbumDetails = DbContext.``[dbo].[AlbumDetails]Entity``