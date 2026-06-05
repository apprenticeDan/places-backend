module Places.Infrastructure.LugaresRepository

open System.Data
open Npgsql
open Dapper
open Places.Domain

let private conexion (connStr: string) : IDbConnection =
    new NpgsqlConnection(connStr) :> IDbConnection

// ─── Lugares ─────────────────────────────────────────────────────────────────

[<CLIMutable>]
type LugarRow = {
    id_lugar    : int
    nombre      : string
    descripcion : string
    rating      : int
}

let obtenerLugares (connStr: string) () =
    async {
        use conn = conexion connStr
        let sql = """
            SELECT l.id_lugar, l.nombre, l.descripcion, 5 as rating
            FROM lugares l
        """
        let! rows = 
            conn.QueryAsync<LugarRow>(sql) 
            |> Async.AwaitTask
            
        let basePath = "http://localhost:8080/api/images"
        
        return 
            rows 
            |> Seq.map (fun r -> 
                { Id = r.id_lugar
                  Nombre = r.nombre
                  Descripcion = r.descripcion
                  Rating = r.rating
                  Images = {
                      Thumb  = sprintf "%s/lugar%d.jpg" basePath r.id_lugar
                      Medium = sprintf "%s/lugar%d.jpg" basePath r.id_lugar
                      Full   = sprintf "%s/lugar%d.jpg" basePath r.id_lugar
                  }
                })
            |> Seq.toList
    }

// ─── Reseñas ─────────────────────────────────────────────────────────────────

[<CLIMutable>]
type ComentarioRow = {
    comentario_id   : int
    nombres         : string
    primer_apellido : string
    comentario      : string
    foto_url        : string
}

let obtenerResenasPorLugar (connStr: string) (placeId: int) =
    async {
        use conn = conexion connStr
        let sql = """
            SELECT c.comentario_id, p.nombres, p.primer_apellido, c.comentario,
                   COALESCE(f.url, 'assets/images/persona1.png') as foto_url
            FROM comentarios c
            JOIN Usuarios u ON c.persona_id = u.id_persona
            JOIN Personas p ON u.id_persona = p.id_persona
            LEFT JOIN fotos f ON c.comentario_id = f.comentario_id
            WHERE c.lugar_id = @placeId
        """
        let! rows = 
            conn.QueryAsync<ComentarioRow>(sql, {| placeId = placeId |}) 
            |> Async.AwaitTask
            
        let basePath = "http://localhost:8080/api/images"
        
        return 
            rows 
            |> Seq.map (fun r -> 
                let imgFile = r.foto_url.Replace("assets/images/", "")
                { Id = r.comentario_id
                  UserName = sprintf "%s %s" r.nombres r.primer_apellido
                  Summary = "1 review - 3 photos" // Hardcoded temporal para diseño
                  Stars = 4 // Temporal
                  CommentText = r.comentario
                  ProfileImageUrl = sprintf "%s/%s" basePath imgFile
                })
            |> Seq.toList
    }
