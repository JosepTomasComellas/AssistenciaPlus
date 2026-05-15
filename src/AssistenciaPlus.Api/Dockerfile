# ── Stage 1: Build ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar fitxers de projecte per aprofitar la caché de NuGet
COPY AssistenciaPlus.Core/AssistenciaPlus.Core.csproj AssistenciaPlus.Core/
COPY AssistenciaPlus.Domain/AssistenciaPlus.Domain.csproj AssistenciaPlus.Domain/
COPY AssistenciaPlus.Application/AssistenciaPlus.Application.csproj AssistenciaPlus.Application/
COPY AssistenciaPlus.Infrastructure/AssistenciaPlus.Infrastructure.csproj AssistenciaPlus.Infrastructure/
COPY AssistenciaPlus.Shared/AssistenciaPlus.Shared.csproj AssistenciaPlus.Shared/
COPY AssistenciaPlus.Api/AssistenciaPlus.Api.csproj AssistenciaPlus.Api/

RUN dotnet restore AssistenciaPlus.Api/AssistenciaPlus.Api.csproj

# Copiar tot el codi font
COPY . .

RUN dotnet publish AssistenciaPlus.Api/AssistenciaPlus.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────
# noble = Ubuntu 24.04 complet (apt-get disponible, adduser disponible)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app

# Instal·lar curl per healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Crear directoris necessaris
RUN mkdir -p /app/logs /app/uploads

COPY --from=build /app/publish .

# Usuari no root per seguretat
RUN adduser --disabled-password --gecos "" appuser \
    && chown -R appuser:appuser /app
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AssistenciaPlus.Api.dll"]
