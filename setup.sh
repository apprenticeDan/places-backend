#!/bin/bash
# Ejecutar UNA vez desde dentro del contenedor: bash /workspace/setup.sh
# Guarda para no ejecutar dos veces
if [ -f "/workspace/src/.scaffold_done" ]; then
  echo "Scaffold ya fue ejecutado. Saliendo."
  exit 0
fi

cd /workspace

# Solución
dotnet new sln -n PlacesBackend

# Domain — tipos puros, sin dependencias externas
dotnet new classlib -lang "F#" -n Places.Domain -o src/Places.Domain
dotnet sln add src/Places.Domain

# Application — pipelines, casos de uso, Result<> flows
dotnet new classlib -lang "F#" -n Places.Application -o src/Places.Application
dotnet sln add src/Places.Application
dotnet add src/Places.Application reference src/Places.Domain

# Infrastructure — BD, JWT, efectos secundarios
dotnet new classlib -lang "F#" -n Places.Infrastructure -o src/Places.Infrastructure
dotnet sln add src/Places.Infrastructure
dotnet add src/Places.Infrastructure reference src/Places.Domain
dotnet add src/Places.Infrastructure reference src/Places.Application

dotnet add src/Places.Infrastructure package Npgsql
dotnet add src/Places.Infrastructure package Dapper
dotnet add src/Places.Infrastructure package BCrypt.Net-Next
dotnet add src/Places.Infrastructure package Microsoft.Extensions.Configuration.Abstractions

# Web — endpoints, entrada/salida HTTP
dotnet new web -lang "F#" -n Places.Web -o src/Places.Web
dotnet sln add src/Places.Web
dotnet add src/Places.Web reference src/Places.Application
dotnet add src/Places.Web reference src/Places.Infrastructure

# Marca de scaffold completado
touch src/.scaffold_done
echo "Scaffold completo."