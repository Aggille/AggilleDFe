# syntax=docker/dockerfile:1
# Build a partir da RAIZ do repositorio:
#   docker build -f AggilleDFe.API/Dockerfile -t aggilledfe-api .
# (o docker-compose.yml na raiz ja faz isso com o context certo)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AggilleDFe.API/AggilleDFe.API.csproj AggilleDFe.API/
COPY AggilleDFe.Application/AggilleDFe.Application.csproj AggilleDFe.Application/
COPY AggilleDFe.Domain/AggilleDFe.Domain.csproj AggilleDFe.Domain/
COPY AggilleDFe.Infrastructure/AggilleDFe.Infrastructure.csproj AggilleDFe.Infrastructure/
RUN dotnet restore AggilleDFe.API/AggilleDFe.API.csproj

COPY AggilleDFe.API/ AggilleDFe.API/
COPY AggilleDFe.Application/ AggilleDFe.Application/
COPY AggilleDFe.Domain/ AggilleDFe.Domain/
COPY AggilleDFe.Infrastructure/ AggilleDFe.Infrastructure/

RUN dotnet publish AggilleDFe.API/AggilleDFe.API.csproj -c Release -o /app/publish --no-restore

# Bundle das migrations (executavel self-contained, nao precisa do SDK na
# imagem final) - roda sozinho contra Postgres antes de subir a API, ver
# AggilleDFe.API/docker-entrypoint.sh
RUN dotnet tool install --global dotnet-ef --version 10.*
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project AggilleDFe.Infrastructure/AggilleDFe.Infrastructure.csproj \
    --startup-project AggilleDFe.API/AggilleDFe.API.csproj \
    --self-contained -r linux-x64 \
    -o /app/publish/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Habilita o "legacy provider" do OpenSSL 3.0 - sem isso, certificados A1
# (.pfx) da ICP-Brasil com algoritmos legados (RC2-40-CBC/3DES) nao carregam
# nesta imagem (Debian, OpenSSL 3.x). Ver DEPLOY_LINUX.md secao 9.
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
COPY AggilleDFe.API/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh /app/efbundle

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["/app/docker-entrypoint.sh"]
