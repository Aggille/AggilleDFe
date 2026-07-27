# syntax=docker/dockerfile:1
# Build a partir da RAIZ do repositorio:
#   docker build -f AggilleDFe.Worker/Dockerfile -t aggilledfe-worker .
# (o docker-compose.yml na raiz ja faz isso com o context certo)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AggilleDFe.Worker/AggilleDFe.Worker.csproj AggilleDFe.Worker/
COPY AggilleDFe.Application/AggilleDFe.Application.csproj AggilleDFe.Application/
COPY AggilleDFe.Domain/AggilleDFe.Domain.csproj AggilleDFe.Domain/
COPY AggilleDFe.Infrastructure/AggilleDFe.Infrastructure.csproj AggilleDFe.Infrastructure/
RUN dotnet restore AggilleDFe.Worker/AggilleDFe.Worker.csproj

COPY AggilleDFe.Worker/ AggilleDFe.Worker/
COPY AggilleDFe.Application/ AggilleDFe.Application/
COPY AggilleDFe.Domain/ AggilleDFe.Domain/
COPY AggilleDFe.Infrastructure/ AggilleDFe.Infrastructure/

RUN dotnet publish AggilleDFe.Worker/AggilleDFe.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Mesmo ajuste do Dockerfile da API - o Worker tambem carrega certificados
# A1 (.pfx) da ICP-Brasil. Ver DEPLOY_LINUX.md secao 9.
RUN { \
        echo ""; \
        echo "[openssl_init]"; \
        echo "providers = provider_sect"; \
        echo ""; \
        echo "[provider_sect]"; \
        echo "default = default_sect"; \
        echo "legacy = legacy_sect"; \
        echo ""; \
        echo "[default_sect]"; \
        echo "activate = 1"; \
        echo ""; \
        echo "[legacy_sect]"; \
        echo "activate = 1"; \
    } >> /etc/ssl/openssl.cnf

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AggilleDFe.Worker.dll"]
