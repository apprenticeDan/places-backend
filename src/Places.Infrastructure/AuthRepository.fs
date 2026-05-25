module Places.Infrastructure.AuthRepository

open System
open System.Data
open Npgsql
open Dapper
open Places.Domain



// ─── Helpers de mapeo ────────────────────────────────────────────────────────
// Dapper devuelve registros anónimos — los convertimos a tipos del dominio

[<CLIMutable>]
type UsuarioRow = {
    id_persona      : int
    usuario         : string
    contrasena      : string  // sin eñe para evitar problemas con Dapper
    id_rol          : int
    nombre_rol      : string
}

let private conexion (connStr: string) : IDbConnection =
    new NpgsqlConnection(connStr) :> IDbConnection

let private agruparRoles (rows: UsuarioRow seq) : Usuario option =
    rows
    |> Seq.toList
    |> function
    | [] -> None
    | first :: _ as all ->
        let roles: Rol list =
            all
            |> List.map (fun r -> { Id = RolId r.id_rol; Nombre = NombreRol r.nombre_rol })
        Some {
            Persona = {
                Id              = PersonaId first.id_persona
                Nombres         = ""   // se puede extender el query si hace falta
                PrimerApellido  = ""
                SegundoApellido = ""
                Email           = Email first.usuario
            }
            NombreUsuario  = Email first.usuario
            HashContraseña = PasswordHash first.contrasena
            Roles          = roles
        }

// ─── Query ───────────────────────────────────────────────────────────────────

let private sqlBuscarUsuario = """
    SELECT u.id_persona, u.usuario, u.contrasena,
           r.id_rol, r.nombre AS nombre_rol
    FROM   Usuarios u
    JOIN   cuentas  c ON c.id_persona = u.id_persona
    JOIN   roles    r ON r.id_rol     = c.id_rol
    WHERE  u.usuario = @usuario
"""

// ─── Implementación del Port ──────────────────────────────────────────────────
// Esta función satisface el tipo BuscarUsuarioPorEmail definido en Application

let buscarUsuarioPorEmail (connStr: string) : Places.Application.BuscarUsuarioPorEmail =
    fun (Email email) ->
        async {
            use conn = conexion connStr
            let! rows =
                conn.QueryAsync<UsuarioRow>(sqlBuscarUsuario, {| usuario = email |})
                |> Async.AwaitTask
            return
                rows
                |> agruparRoles
                |> Option.map Ok
                |> Option.defaultValue (Error UsuarioNoEncontrado)
        }