module Places.Web.AuthEndpoints

open Microsoft.AspNetCore.Http
open Places.Application

// ─── DTO de entrada ───────────────────────────────────────────────────────────

[<CLIMutable>]
type LoginRequest = {
    email      : string
    contrasena : string
}

// ─── Handler ──────────────────────────────────────────────────────────────────
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