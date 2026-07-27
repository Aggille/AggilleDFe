# syntax=docker/dockerfile:1
# Build a partir da RAIZ do repositorio:
#   docker build -f AggilleDFe.Web/Dockerfile -t aggilledfe-web .
# (o docker-compose.yml na raiz ja faz isso com o context certo)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AggilleDFe.Web/AggilleDFe.Web.csproj AggilleDFe.Web/
COPY AggilleDFe.Application/AggilleDFe.Application.csproj AggilleDFe.Application/
RUN dotnet restore AggilleDFe.Web/AggilleDFe.Web.csproj

COPY AggilleDFe.Web/ AggilleDFe.Web/
COPY AggilleDFe.Application/ AggilleDFe.Application/

RUN dotnet publish AggilleDFe.Web/AggilleDFe.Web.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY AggilleDFe.Web/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY AggilleDFe.Web/docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 80
ENTRYPOINT ["/entrypoint.sh"]
