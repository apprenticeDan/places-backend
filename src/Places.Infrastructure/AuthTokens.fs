module Places.Infrastructure.AuthTokens

open System
open System.Text
open System.Security.Claims
open Microsoft.IdentityModel.Tokens
open System.IdentityModel.Tokens.Jwt
open Places.Domain

// ─── BCrypt ───────────────────────────────────────────────────────────────────
// Satisface el tipo VerificarHash de Application — función pura de dos argumentos

let verificarHash (candidata: string) (PasswordHash hash: PasswordHash) : bool =
    BCrypt.Net.BCrypt.Verify(candidata, hash)

// ─── JWT ──────────────────────────────────────────────────────────────────────
// Satisface el tipo EmitirToken de Application
// La clave y expiración vienen de config — no están hardcodeadas

let emitirToken (secretKey: string) (expiresHours: int) : Places.Application.EmitirToken =
    fun (usuario: Usuario) ->
        let (Email email) = usuario.NombreUsuario

        let claims =
            usuario.Roles
            |> List.map (fun r -> let (NombreRol n) = r.Nombre in Claim(ClaimTypes.Role, n))
            |> List.append [ Claim(ClaimTypes.Name, email) ]

        let key   = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        let creds = SigningCredentials(key, SecurityAlgorithms.HmacSha256)

        let token =
            JwtSecurityToken(
                issuer    = "places-api",
                audience  = "places-client",
                claims    = claims,
                expires   = DateTime.UtcNow.AddHours(float expiresHours),
                signingCredentials = creds
            )

        JwtSecurityTokenHandler().WriteToken(token)

let hashPassword (candidata: string) : string =
    BCrypt.Net.BCrypt.HashPassword(candidata)