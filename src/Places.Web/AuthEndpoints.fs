module Places.Web.AuthEndpoints

open Microsoft.AspNetCore.Http
open Places.Application

// ─── DTO de entrada (Login) ───────────────────────────────────────────────────

[<CLIMutable>]
type LoginRequest = {
    email      : string
    contrasena : string
}

// ─── DTO de entrada (Registro) ────────────────────────────────────────────────

[<CLIMutable>]
type RegisterRequest = {
    nombres          : string
    primer_apellido  : string
    segundo_apellido : string
    ci               : int
    complemento      : string   // LP, CB, TJ, PT, OR, SC, PN, CH
    fecha_nacimiento : string   // formato "YYYY-MM-DD"
    genero           : string
    direccion        : string
    telefono_fijo    : int
    celular          : int
    email            : string
    contrasena       : string
}

// ─── Handler Login ────────────────────────────────────────────────────────────
// Usamos parámetros tipados en vez de HttpContext para habilitar OpenAPI

let loginHandler
    (ejecutarLogin: LoginCommand -> Async<LoginResponse>)
    (req: LoginRequest)
    : System.Threading.Tasks.Task<IResult> =
    task {
        let cmd = {
            EmailRaw   = req.email
            Contraseña = req.contrasena
        }

        let! respuesta = ejecutarLogin cmd |> Async.StartAsTask

        match respuesta with
        | LoginOk r ->
            return Results.Ok({| token = r.Token; usuario = r.Usuario; roles = r.Roles |})
        | Unauthorized msg ->
            return Results.Json({| error = msg |}, statusCode = 401)
        | BadRequest msg ->
            return Results.BadRequest({| error = msg |})
        | ServerError msg ->
            return Results.Json({| error = msg |}, statusCode = 500)
    }

// ─── Handler Registro ─────────────────────────────────────────────────────────

let registroHandler
    (ejecutarRegistro: RegisterCommand -> Async<RegistroResponse>)
    (req: RegisterRequest)
    : System.Threading.Tasks.Task<IResult> =
    task {
        let fechaNac =
            match System.DateTime.TryParse(req.fecha_nacimiento) with
            | true, d  -> d
            | false, _ -> System.DateTime(2000, 1, 1)

        let cmd : RegisterCommand = {
            Nombres         = req.nombres
            PrimerApellido  = req.primer_apellido
            SegundoApellido = req.segundo_apellido
            CI              = req.ci
            Complemento     = req.complemento
            FechaNacimiento = fechaNac
            Genero          = req.genero
            Direccion       = req.direccion
            TelefonoFijo    = req.telefono_fijo
            Celular         = req.celular
            EmailRaw        = req.email
            Contraseña      = req.contrasena
        }

        let! respuesta = ejecutarRegistro cmd |> Async.StartAsTask

        match respuesta with
        | RegistroOk msg ->
            return Results.Created("", {| mensaje = msg |})
        | RegistroBad msg ->
            return Results.BadRequest({| error = msg |})
        | RegistroConflict msg ->
            return Results.Conflict({| error = msg |})
        | RegistroError msg ->
            return Results.Json({| error = msg |}, statusCode = 500)
    }