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

// ═══════════════════════════════════════════════════════════════════════════════
// ─── REGISTRO: Funciones de Infrastructure para crear usuarios ────────────────
// ═══════════════════════════════════════════════════════════════════════════════

// ─── Port: EmailExiste ────────────────────────────────────────────────────────
// Verifica si ya existe un usuario con ese email en la tabla Usuarios

let private sqlEmailExiste = """
    SELECT COUNT(1) FROM Usuarios WHERE usuario = @usuario
"""

let emailExiste (connStr: string) : Places.Application.EmailExiste =
    fun (Email email) ->
        async {
            use conn = conexion connStr
            let! count =
                conn.ExecuteScalarAsync<int>(sqlEmailExiste, {| usuario = email |})
                |> Async.AwaitTask
            return count > 0
        }

// ─── Port: CrearUsuario ──────────────────────────────────────────────────────
// Inserta Persona, Usuario y asigna rol "Usuario Común" (id_rol=2)
// dentro de una transacción para mantener consistencia

let private sqlNextId = """
    SELECT COALESCE(MAX(id_persona), 0) + 1 FROM Personas
"""

let private sqlInsertPersona = """
    INSERT INTO Personas (id_persona, nombres, primer_apellido, segundo_apellido,
                          CI, complemento, fecha_nacimiento, genero, direccion,
                          telefono_fijo, celular, email)
    VALUES (@id, @nombres, @apellido1, @apellido2,
            @ci, @complemento, @fechaNac, @genero, @direccion,
            @telFijo, @celular, @email)
"""

let private sqlInsertUsuario = """
    INSERT INTO Usuarios (id_persona, usuario, contrasena)
    VALUES (@id, @usuario, @contrasena)
"""

let private sqlInsertCuenta = """
    INSERT INTO cuentas (id_persona, id_rol)
    VALUES (@id, 2)
"""
// ↑ id_rol = 2 es "Usuario Común" según el script de semillas

let crearUsuario (connStr: string) : Places.Application.CrearUsuario =
    fun nombres apellido1 apellido2 ci complemento fechaNac genero direccion telFijo celular email hashPwd ->
        async {
            try
                use conn = new NpgsqlConnection(connStr)
                do! conn.OpenAsync() |> Async.AwaitTask

                use txn = conn.BeginTransaction()

                // Obtener siguiente id
                let! nextId =
                    conn.ExecuteScalarAsync<int>(sqlNextId, transaction = txn)
                    |> Async.AwaitTask

                // Insertar en Personas
                let personaParams = {|
                    id = nextId
                    nombres = nombres
                    apellido1 = apellido1
                    apellido2 = apellido2
                    ci = ci
                    complemento = complemento
                    fechaNac = fechaNac
                    genero = genero
                    direccion = direccion
                    telFijo = telFijo
                    celular = celular
                    email = email
                |}
                do! conn.ExecuteAsync(sqlInsertPersona, personaParams, txn)
                    |> Async.AwaitTask |> Async.Ignore

                // Insertar en Usuarios
                let usuarioParams = {|
                    id = nextId
                    usuario = email
                    contrasena = hashPwd
                |}
                do! conn.ExecuteAsync(sqlInsertUsuario, usuarioParams, txn)
                    |> Async.AwaitTask |> Async.Ignore

                // Insertar cuenta con rol Usuario Común
                do! conn.ExecuteAsync(sqlInsertCuenta, {| id = nextId |}, txn)
                    |> Async.AwaitTask |> Async.Ignore

                do! txn.CommitAsync() |> Async.AwaitTask

                return Ok ()
            with ex ->
                return Error (ErrorInterno (ex.Message))
        }