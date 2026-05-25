module Places.Web.Program

open System
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.OpenApi
open Scalar.AspNetCore
open Places.Application
open Places.Infrastructure

[<EntryPoint>]
let main argv =
    let builder = WebApplication.CreateBuilder(argv)
    let config  = builder.Configuration

    let connStr   = config.["ConnectionString"]
    let jwtKey    = config.["Jwt:Key"]
    let jwtHours  = config.["Jwt:ExpiresHours"] |> int

    // ─── Construir las funciones concretas (inyección funcional) ─────────────────
    let buscar   = AuthRepository.buscarUsuarioPorEmail connStr
    let verificar = AuthTokens.verificarHash
    let emitir   = AuthTokens.emitirToken jwtKey jwtHours

    // ─── Caso de uso ensamblado (Login) ───────────────────────────────────────────
    let ejecutarLogin = AuthUseCase.loginResponse buscar verificar emitir

    // ─── Funciones concretas para Registro ────────────────────────────────────────
    let existeEmail    = AuthRepository.emailExiste connStr
    let hashPwd        = AuthTokens.hashPassword
    let crearUsr       = AuthRepository.crearUsuario connStr

    // ─── Caso de uso ensamblado (Registro) ────────────────────────────────────────
    let ejecutarRegistro = AuthUseCase.registrarResponse existeEmail hashPwd crearUsr

    // ─── JWT middleware ───────────────────────────────────────────────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun opts ->
            opts.TokenValidationParameters <- TokenValidationParameters(
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = "places-api",
                ValidAudience            = "places-client",
                IssuerSigningKey         = SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey))
            )) |> ignore
    builder.Services.AddAuthorization() |> ignore

    // ─── CORS configuration ───────────────────────────────────────────────────────
    builder.Services.AddCors(fun options ->
        options.AddPolicy("AllowAll", fun policy ->
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader() |> ignore
        )
    ) |> ignore

    //builder.Services.AddEndpointsApiExplorer() |> ignore
    //builder.Services.AddSwaggerGen()           |> ignore
    builder.Services.AddOpenApi() |> ignore

    let app = builder.Build()
    app.MapOpenApi()  |> ignore
    app.MapScalarApiReference() |> ignore

    app.UseCors("AllowAll") |> ignore

    app.UseAuthentication() |> ignore
    app.UseAuthorization()  |> ignore
    //app.UseSwagger()   |> ignore
    //app.UseSwaggerUI() |> ignore
    

    // ─── Endpoints ────────────────────────────────────────────────────────────────
    app.MapPost("/auth/login", RequestDelegate(fun ctx ->
        AuthEndpoints.loginHandler ejecutarLogin ctx))
        .WithName("Login")
        .WithTags("Auth")
        .Accepts<Places.Web.AuthEndpoints.LoginRequest>("application/json")
        .Produces(200)
        .Produces(401)
        .Produces(400)
        |> ignore

    app.MapPost("/auth/registro", RequestDelegate(fun ctx ->
        AuthEndpoints.registroHandler ejecutarRegistro ctx))
        .WithName("Registro")
        .WithTags("Auth")
        .Accepts<Places.Web.AuthEndpoints.RegisterRequest>("application/json")
        .Produces(201)
        .Produces(400)
        .Produces(409)
        |> ignore

    //if app.Environment.IsDevelopment() then 
    app.MapGet("/dev/hash", RequestDelegate(fun ctx ->
        task {
            let pwd  = ctx.Request.Query["pwd"].ToString()
            let hash = AuthTokens.hashPassword pwd
            do! ctx.Response.WriteAsJsonAsync({| hash = hash |})
        } :> System.Threading.Tasks.Task
    )) |> ignore
    app.Run()
    0// Exit code

