# ── Stage 1: Build ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ARG APP_PUBLIC_URL=https://localhost

COPY AssistenciaPlus.Shared/AssistenciaPlus.Shared.csproj AssistenciaPlus.Shared/
COPY AssistenciaPlus.Web/AssistenciaPlus.Web.csproj AssistenciaPlus.Web/

RUN dotnet restore AssistenciaPlus.Web/AssistenciaPlus.Web.csproj

COPY . .

RUN dotnet publish AssistenciaPlus.Web/AssistenciaPlus.Web.csproj \
    -c Release -o /app/publish --no-restore \
    /p:ApiBaseUrl=${APP_PUBLIC_URL}/api

# ── Stage 2: Runtime nginx ────────────────────────────────
FROM nginx:1.25-alpine AS final

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY AssistenciaPlus.Web/nginx-blazor.conf /etc/nginx/conf.d/default.conf

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1
