namespace Places.Application

open Places.Domain

// ─── Ports (contratos que Infrastructure debe implementar) ────────────────────
// Son funciones, no interfaces. El dominio funcional no necesita clases abstractas.

type BuscarUsuarioPorEmail = Email -> Async<Result<Usuario, AuthError>>
type VerificarHash         = string -> PasswordHash -> bool
type EmitirToken           = Usuario -> string

// ─── Ports de Registro ────────────────────────────────────────────────────────
type EmailExiste  = Email -> Async<bool>
type HashPassword = string -> string
type CrearUsuario = string -> string -> string -> int -> string -> System.DateTime -> string -> string -> int -> int -> string -> string -> Async<Result<unit, AuthError>>
//                  nombres   apellido   apellido2   CI   complemento   fechaNac   genero   direccion   telFijo   celular   email   hashPassword

// ─── Comando de entrada (Login) ───────────────────────────────────────────────

type LoginCommand = {
    EmailRaw    : string
    Contraseña  : string
}

// ─── Comando de entrada (Registro) ────────────────────────────────────────────

type RegisterCommand = {
    Nombres         : string
    PrimerApellido  : string
    SegundoApellido : string
    CI              : int
    Complemento     : string   // LP, CB, TJ, PT, OR, SC, PN, CH
    FechaNacimiento : System.DateTime
    Genero          : string
    Direccion       : string
    TelefonoFijo    : int
    Celular         : int
    EmailRaw        : string
    Contraseña      : string
}

// ─── Resultado de salida (Login) ──────────────────────────────────────────────

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

// ─── Resultado de salida (Registro) ───────────────────────────────────────────

type RegistroResponse =
    | RegistroOk     of string   // mensaje de éxito
    | RegistroBad    of string   // validación fallida
    | RegistroConflict of string // email duplicado
    | RegistroError  of string   // error interno

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
                | Error EmailYaExiste      -> BadRequest   "Email ya registrado"
                | Error (ErrorInterno msg) -> ServerError  msg
        }

    // ─── Caso de uso: Registro ────────────────────────────────────────────────

    let registrar
        (emailExiste   : EmailExiste)
        (hashPassword  : HashPassword)
        (crearUsuario  : CrearUsuario)
        (cmd           : RegisterCommand)
        : Async<Result<string, AuthError>> =
        async {
            // paso 1 — validar email
            match Validacion.validarEmail cmd.EmailRaw with
            | Error e -> return Error e
            | Ok (Email emailStr as email) ->

            // paso 2 — validar contraseña
            match Validacion.validarContraseñaRegistro cmd.Contraseña with
            | Error e -> return Error e
            | Ok pwd ->

            // paso 3 — verificar que el email no exista ya
            let! existe = emailExiste email
            if existe then
                return Error EmailYaExiste
            else

            // paso 4 — hashear contraseña
            let hashed = hashPassword pwd

            // paso 5 — crear usuario en BD
            let! resultado =
                crearUsuario
                    cmd.Nombres cmd.PrimerApellido cmd.SegundoApellido
                    cmd.CI cmd.Complemento cmd.FechaNacimiento
                    cmd.Genero cmd.Direccion cmd.TelefonoFijo cmd.Celular
                    emailStr hashed

            return
                match resultado with
                | Ok () -> Ok (sprintf "Usuario %s registrado con éxito" emailStr)
                | Error e -> Error e
        }

    let registrarResponse
        (emailExiste  : EmailExiste)
        (hashPassword : HashPassword)
        (crearUsuario : CrearUsuario)
        (cmd          : RegisterCommand)
        : Async<RegistroResponse> =
        async {
            let! resultado = registrar emailExiste hashPassword crearUsuario cmd
            return
                match resultado with
                | Ok msg                   -> RegistroOk msg
                | Error (EmailInvalido m)  -> RegistroBad (sprintf "Email inválido: %s" m)
                | Error ContraseñaDebil    -> RegistroBad "La contraseña debe tener al menos 6 caracteres"
                | Error EmailYaExiste      -> RegistroConflict "Este correo electrónico ya está registrado"
                | Error (ErrorInterno msg) -> RegistroError msg
                | Error _                  -> RegistroError "Error inesperado"
        }