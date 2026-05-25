namespace Places.Application

open Places.Domain

// ─── Ports (contratos que Infrastructure debe implementar) ────────────────────
// Son funciones, no interfaces. El dominio funcional no necesita clases abstractas.

type BuscarUsuarioPorEmail = Email -> Async<Result<Usuario, AuthError>>
type VerificarHash         = string -> PasswordHash -> bool
type EmitirToken           = Usuario -> string

// ─── Comando de entrada ───────────────────────────────────────────────────────

type LoginCommand = {
    EmailRaw    : string
    Contraseña  : string
}

// ─── Resultado de salida ──────────────────────────────────────────────────────

type LoginResult = {
    Token   : string
    Usuario : string
    Roles   : string list
}

type LoginResponse =
    | LoginOk      of LoginResult
    | Unauthorized of string
    | BadRequest   of string
    | ServerError  of string

// ─── Pipeline ─────────────────────────────────────────────────────────────────

module AuthUseCase =

    let login
        (buscarUsuario : BuscarUsuarioPorEmail)
        (verificarHash : VerificarHash)
        (emitirToken   : EmitirToken)
        (cmd           : LoginCommand)
        : Async<Result<LoginResult, AuthError>> =

        async {
            // paso 1 — validar que el email tiene forma correcta (puro, dominio)
            let emailResult = Validacion.validarEmail cmd.EmailRaw

            match emailResult with
            | Error e -> return Error e
            | Ok email ->

            // paso 2 — buscar usuario en la BD (efecto, delegado a Infrastructure)
            let! usuarioResult = buscarUsuario email

            match usuarioResult with
            | Error e -> return Error e
            | Ok usuario ->

            // paso 3 — verificar contraseña (puro, función de BCrypt inyectada)
            let hashCheck =
                Validacion.verificarContraseña
                    cmd.Contraseña
                    verificarHash
                    usuario.HashContraseña

            match hashCheck with
            | Error e -> return Error e
            | Ok () ->

            // paso 4 — emitir token (efecto, delegado a Infrastructure)
            let token = emitirToken usuario

            let (Email emailStr) = usuario.NombreUsuario

            return Ok {
                Token   = token
                Usuario = emailStr
                Roles   = usuario.Roles
                          |> List.map (fun r -> let (NombreRol n) = r.Nombre in n)
            }
        }

    // función nueva — traduce Result a respuesta HTTP neutral
    let loginResponse
        (buscarUsuario : BuscarUsuarioPorEmail)
        (verificarHash : VerificarHash)
        (emitirToken   : EmitirToken)
        (cmd           : LoginCommand)
        : Async<LoginResponse> =
        async {
            let! resultado = login buscarUsuario verificarHash emitirToken cmd
            return
                match resultado with
                | Ok r                    -> LoginOk r
                | Error CredencialesInvalidas
                | Error UsuarioNoEncontrado -> Unauthorized "Credenciales inválidas"
                | Error (EmailInvalido _)  -> BadRequest   "Email inválido"
                | Error (SinPrivilegios _) -> Unauthorized "Sin privilegios"
                | Error ContraseñaDebil    -> BadRequest   "Contraseña débil"
        }