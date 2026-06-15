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

    builder.Services.AddOpenApi() |> ignore

    let app = builder.Build()
    app.MapOpenApi()  |> ignore
    app.MapScalarApiReference() |> ignore

    app.UseCors("AllowAll") |> ignore

    app.UseAuthentication() |> ignore
    app.UseAuthorization()  |> ignore

    // ─── Endpoints ────────────────────────────────────────────────────────────────
    // Al usar System.Func el framework puede leer los tipos automáticamente (OpenAPI)
    
    app.MapPost("/auth/login", Func<Places.Web.AuthEndpoints.LoginRequest, System.Threading.Tasks.Task<IResult>>(AuthEndpoints.loginHandler ejecutarLogin))
        .WithName("Login")
        .WithTags("Auth")
        .Produces(200)
        .Produces(401)
        .Produces(400)
        |> ignore

    app.MapPost("/auth/registro", Func<Places.Web.AuthEndpoints.RegisterRequest, System.Threading.Tasks.Task<IResult>>(AuthEndpoints.registroHandler ejecutarRegistro))
        .WithName("Registro")
        .WithTags("Auth")
        .Produces(201)
        .Produces(400)
        .Produces(409)
        |> ignore

    app.MapGet("/dev/hash", RequestDelegate(fun ctx ->
        task {
            let pwd  = ctx.Request.Query["pwd"].ToString()
            let hash = AuthTokens.hashPassword pwd
            do! ctx.Response.WriteAsJsonAsync({| hash = hash |})
        } :> System.Threading.Tasks.Task
    )) |> ignore
    
    // ─── Static Files (Imágenes) ───────────────────────────────────────────────────
    app.UseStaticFiles(StaticFileOptions(
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            System.IO.Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "images")
        ),
        RequestPath = "/api/images"
    )) |> ignore

    // ─── Endpoints Lugares y Reseñas ──────────────────────────────────────────────
    let obtenerLugares = LugaresRepository.obtenerLugares connStr
    let obtenerResenas = LugaresRepository.obtenerResenasPorLugar connStr

    app.MapGet("/api/places", Func<System.Threading.Tasks.Task<IResult>>(fun () ->
        task {
            let! lugares = obtenerLugares () |> Async.StartAsTask
            return Results.Ok(lugares)
        }))
        .WithName("GetPlaces")
        .WithTags("Places")
        |> ignore

    app.MapGet("/api/places/{placeId}/reviews", Func<int, System.Threading.Tasks.Task<IResult>>(fun placeId ->
        task {
            let! resenas = obtenerResenas placeId |> Async.StartAsTask
            return Results.Ok(resenas)
        }))
        .WithName("GetReviews")
        .WithTags("Places")
        |> ignore

    // ─── Crear Reseña (requiere JWT) ──────────────────────────────────────────
    let obtenerPersonaId = LugaresRepository.obtenerPersonaIdPorEmail connStr
    let crearComentario  = LugaresRepository.crearComentario connStr

    app.MapPost("/api/places/{placeId}/reviews", Func<int, HttpContext, System.Threading.Tasks.Task<IResult>>(fun placeId ctx ->
        task {
            let email = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Name)
            if email = null then
                return Results.Unauthorized()
            else
                let! personaId = obtenerPersonaId email.Value |> Async.StartAsTask
                if personaId = 0 then
                    return Results.BadRequest({| error = "Usuario no encontrado" |})
                else
                    let! body = ctx.Request.ReadFromJsonAsync<Places.Domain.NuevoComentario>()
                    if body.Estrellas < 1 || body.Estrellas > 5 then
                        return Results.BadRequest({| error = "Estrellas debe ser entre 1 y 5" |})
                    else
                        do! crearComentario personaId placeId body.Texto body.Estrellas |> Async.StartAsTask
                        return Results.Created("", {| mensaje = "Reseña creada" |})
        }))
        .RequireAuthorization()
        .WithName("CreateReview")
        .WithTags("Places")
        |> ignore

    // ─── Favoritos (requieren JWT) ────────────────────────────────────────────
    let agregarFav  = LugaresRepository.agregarFavorito connStr
    let quitarFav   = LugaresRepository.quitarFavorito connStr
    let obtenerFavs = LugaresRepository.obtenerFavoritos connStr

    app.MapPost("/api/places/{placeId}/favorite", Func<int, HttpContext, System.Threading.Tasks.Task<IResult>>(fun placeId ctx ->
        task {
            let email = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Name)
            if email = null then return Results.Unauthorized()
            else
                let! personaId = obtenerPersonaId email.Value |> Async.StartAsTask
                if personaId = 0 then return Results.BadRequest({| error = "Usuario no encontrado" |})
                else
                    do! agregarFav personaId placeId |> Async.StartAsTask
                    return Results.Ok({| mensaje = "Favorito agregado" |})
        }))
        .RequireAuthorization()
        .WithName("AddFavorite")
        .WithTags("Favorites")
        |> ignore

    app.MapDelete("/api/places/{placeId}/favorite", Func<int, HttpContext, System.Threading.Tasks.Task<IResult>>(fun placeId ctx ->
        task {
            let email = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Name)
            if email = null then return Results.Unauthorized()
            else
                let! personaId = obtenerPersonaId email.Value |> Async.StartAsTask
                if personaId = 0 then return Results.BadRequest({| error = "Usuario no encontrado" |})
                else
                    do! quitarFav personaId placeId |> Async.StartAsTask
                    return Results.Ok({| mensaje = "Favorito eliminado" |})
        }))
        .RequireAuthorization()
        .WithName("RemoveFavorite")
        .WithTags("Favorites")
        |> ignore

    app.MapGet("/api/favorites", Func<HttpContext, System.Threading.Tasks.Task<IResult>>(fun ctx ->
        task {
            let email = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Name)
            if email = null then return Results.Unauthorized()
            else
                let! personaId = obtenerPersonaId email.Value |> Async.StartAsTask
                if personaId = 0 then return Results.BadRequest({| error = "Usuario no encontrado" |})
                else
                    let! favs = obtenerFavs personaId |> Async.StartAsTask
                    return Results.Ok(favs)
        }))
        .RequireAuthorization()
        .WithName("GetFavorites")
        .WithTags("Favorites")
        |> ignore

    app.Run()
    0
