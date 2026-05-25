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
// Recibe las funciones ya construidas desde Program.fs — sin acceso directo a config

let loginHandler
    (ejecutarLogin: LoginCommand -> Async<LoginResponse>)
    (ctx: HttpContext)
    : System.Threading.Tasks.Task =
    task {
        let! req = ctx.Request.ReadFromJsonAsync<LoginRequest>()

        let cmd = {
            EmailRaw   = req.email
            Contraseña = req.contrasena
        }

        let! respuesta = ejecutarLogin cmd |> Async.StartAsTask

        match respuesta with
        | LoginOk r ->
            ctx.Response.StatusCode <- 200
            do! ctx.Response.WriteAsJsonAsync(
                {| token = r.Token; usuario = r.Usuario; roles = r.Roles |})
        | Unauthorized msg ->
            ctx.Response.StatusCode <- 401
            do! ctx.Response.WriteAsJsonAsync({| error = msg |})
        | BadRequest msg ->
            ctx.Response.StatusCode <- 400
            do! ctx.Response.WriteAsJsonAsync({| error = msg |})
        | ServerError msg ->
            ctx.Response.StatusCode <- 500
            do! ctx.Response.WriteAsJsonAsync({| error = msg |})
    }

// ─── Handler Registro ─────────────────────────────────────────────────────────

let registroHandler
    (ejecutarRegistro: RegisterCommand -> Async<RegistroResponse>)
    (ctx: HttpContext)
    : System.Threading.Tasks.Task =
    task {
        let! req = ctx.Request.ReadFromJsonAsync<RegisterRequest>()

        // Parsear la fecha de nacimiento desde string
        let fechaNac =
            match System.DateTime.TryParse(req.fecha_nacimiento) with
            | true, d  -> d
            | false, _ -> System.DateTime(2000, 1, 1) // valor por defecto seguro

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
            ctx.Response.StatusCode <- 201
            do! ctx.Response.WriteAsJsonAsync({| mensaje = msg |})
        | RegistroBad msg ->
            ctx.Response.StatusCode <- 400
            do! ctx.Response.WriteAsJsonAsync({| error = msg |})
        | RegistroConflict msg ->
            ctx.Response.StatusCode <- 409
            do! ctx.Response.WriteAsJsonAsync({| error = msg |})
        | RegistroError msg ->
            ctx.Response.StatusCode <- 500
            do! ctx.Response.WriteAsJsonAsync({| error = msg |})
    }