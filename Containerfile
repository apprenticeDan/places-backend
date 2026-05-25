FROM mcr.microsoft.com/dotnet/sdk:10.0

# desactivar telemetría también en build time
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

WORKDIR /workspace
EXPOSE 8080