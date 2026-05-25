namespace Places.Domain

open System.Text.RegularExpressions

// ─── Primitivos ──────────────────────────────────────────────────────────────
// Cada valor del dominio tiene su propio tipo — el compilador impide mezclarlos

type PersonaId    = PersonaId    of int
type Email        = Email        of string
type PasswordHash = PasswordHash of string
type NombreRol    = NombreRol    of string
type NombreFunc   = NombreFunc   of string
type RolId        = RolId        of int
type FuncId       = FuncId       of int

// ─── Entidades del dominio ────────────────────────────────────────────────────

type Rol = {
    Id     : RolId
    Nombre : NombreRol
}

type Funcionalidad = {
    Id     : FuncId
    Nombre : NombreFunc
}

type Privilegio = {
    Rol           : Rol
    Funcionalidad : Funcionalidad
}

type Persona = {
    Id              : PersonaId
    Nombres         : string
    PrimerApellido  : string
    SegundoApellido : string
    Email           : Email
}

type Usuario = {
    Persona      : Persona
    NombreUsuario: Email        // el sistema usa email como usuario
    HashContraseña: PasswordHash
    Roles        : Rol list
}

// ─── Errores del dominio ──────────────────────────────────────────────────────

type AuthError =
    | CredencialesInvalidas
    | UsuarioNoEncontrado
    | SinPrivilegios       of NombreFunc
    | EmailInvalido        of string
    | ContraseñaDebil
    | EmailYaExiste        // ← NUEVO: el correo ya fue registrado
    | ErrorInterno         of string  // ← NUEVO: errores inesperados de BD/runtime

// ─── Funciones puras de validación ───────────────────────────────────────────
// Sin efectos secundarios. Sin dependencias externas. Solo lógica.

module Validacion =

    // Regex que valida: usuario@dominio.extension  (mínimo 2 chars en extensión)
    let private emailRegex =
        Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)

    let validarEmail (raw: string) : Result<Email, AuthError> =
        if System.String.IsNullOrWhiteSpace(raw) then
            Error (EmailInvalido "El correo no puede estar vacío")
        elif not (emailRegex.IsMatch(raw.Trim())) then
            Error (EmailInvalido raw)
        else
            Ok (Email (raw.Trim().ToLowerInvariant()))

    let tienePrivilegio (func: NombreFunc) (usuario: Usuario) : Result<Usuario, AuthError> =
        // busca si alguno de los roles del usuario tiene el privilegio requerido
        // la lógica de qué funcionalidades tiene cada rol viene de Infrastructure
        Ok usuario  // placeholder — se completa cuando Application llama con privilegios cargados

    let verificarContraseña
        (candidata: string)
        (verificar: string -> PasswordHash -> bool)  // función inyectada desde Infrastructure
        (hash: PasswordHash)
        : Result<unit, AuthError> =
        if verificar candidata hash
        then Ok ()
        else Error CredencialesInvalidas

    /// Valida que la contraseña tenga al menos 6 caracteres
    let validarContraseñaRegistro (pwd: string) : Result<string, AuthError> =
        if System.String.IsNullOrWhiteSpace(pwd) || pwd.Length < 6 then
            Error ContraseñaDebil
        else
            Ok pwd