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
    estrellas       : int
    foto_url        : string
}

let obtenerResenasPorLugar (connStr: string) (placeId: int) =
    async {
        use conn = conexion connStr
        let sql = """
            SELECT c.comentario_id, p.nombres, p.primer_apellido, c.comentario,
                   COALESCE(c.estrellas, 3) as estrellas,
                   COALESCE(f.url, 'assets/images/persona1.png') as foto_url
            FROM comentarios c
            JOIN Usuarios u ON c.persona_id = u.id_persona
            JOIN Personas p ON u.id_persona = p.id_persona
            LEFT JOIN fotos f ON c.comentario_id = f.comentario_id
            WHERE c.lugar_id = @placeId
            ORDER BY c.fecha_com DESC
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
                  Summary = "1 review - 3 photos"
                  Stars = r.estrellas
                  CommentText = r.comentario
                  ProfileImageUrl = sprintf "%s/%s" basePath imgFile
                })
            |> Seq.toList
    }

// ─── Resolver persona_id desde email ─────────────────────────────────────────

let obtenerPersonaIdPorEmail (connStr: string) (email: string) =
    async {
        use conn = conexion connStr
        let sql = "SELECT id_persona FROM usuarios WHERE usuario = @email"
        let! result = 
            conn.QueryFirstOrDefaultAsync<int>(sql, {| email = email |})
            |> Async.AwaitTask
        return result
    }

// ─── Crear Comentario ────────────────────────────────────────────────────────

let crearComentario (connStr: string) (personaId: int) (lugarId: int) (texto: string) (estrellas: int) =
    async {
        use conn = conexion connStr
        let sql = """
            INSERT INTO comentarios (comentario, fecha_com, persona_id, lugar_id, estrellas)
            VALUES (@texto, CURRENT_DATE, @personaId, @lugarId, @estrellas)
        """
        let! _ = 
            conn.ExecuteAsync(sql, {| texto = texto; personaId = personaId; lugarId = lugarId; estrellas = estrellas |})
            |> Async.AwaitTask
        return ()
    }

// ─── Favoritos ───────────────────────────────────────────────────────────────

let agregarFavorito (connStr: string) (personaId: int) (lugarId: int) =
    async {
        use conn = conexion connStr
        let sql = """
            INSERT INTO favoritos (persona_id, lugar_id)
            VALUES (@personaId, @lugarId)
            ON CONFLICT DO NOTHING
        """
        let! _ = 
            conn.ExecuteAsync(sql, {| personaId = personaId; lugarId = lugarId |})
            |> Async.AwaitTask
        return ()
    }

let quitarFavorito (connStr: string) (personaId: int) (lugarId: int) =
    async {
        use conn = conexion connStr
        let sql = "DELETE FROM favoritos WHERE persona_id = @personaId AND lugar_id = @lugarId"
        let! _ = 
            conn.ExecuteAsync(sql, {| personaId = personaId; lugarId = lugarId |})
            |> Async.AwaitTask
        return ()
    }

let obtenerFavoritos (connStr: string) (personaId: int) =
    async {
        use conn = conexion connStr
        let sql = "SELECT lugar_id FROM favoritos WHERE persona_id = @personaId"
        let! rows = 
            conn.QueryAsync<int>(sql, {| personaId = personaId |})
            |> Async.AwaitTask
        return rows |> Seq.toList
    }

