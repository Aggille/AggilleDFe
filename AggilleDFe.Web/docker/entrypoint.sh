#!/bin/sh
set -e

# O Blazor WASM le a URL da API de wwwroot/appsettings.json em tempo de
# execucao (fetch, no navegador do usuario) - por isso da pra trocar so com
# uma variavel de ambiente do container, sem precisar recompilar o Web.
# Vazio (padrao) = API na mesma origem da pagina, via proxy reverso /api/ do
# nginx.conf - funciona por localhost, IP da rede, IP externo ou dominio.
API_URL="${API_URL:-}"

cat > /usr/share/nginx/html/appsettings.json <<EOF
{ "ApiUrl": "$API_URL" }
EOF

# Remove variantes pre-comprimidas geradas pelo "dotnet publish" com o valor
# original do wwwroot - com gzip_static on no nginx.conf, o nginx serviria
# esse .gz/.br antigo (com o ApiUrl de build) em vez do arquivo acima recem
# reescrito, ja que o navegador sempre aceita gzip.
rm -f /usr/share/nginx/html/appsettings.json.gz /usr/share/nginx/html/appsettings.json.br

# O DNS interno muda conforme o runtime (Docker usa 127.0.0.11; Podman, comum
# no CentOS/RHEL, usa o IP do gateway da rede) - pega o nameserver real do
# container pro "resolver" do proxy /api/ no nginx.conf.
RESOLVER=$(awk '/^nameserver/ { ip = $2; if (ip ~ /:/) ip = "[" ip "]"; print ip; exit }' /etc/resolv.conf)
sed -i "s|__RESOLVER__|${RESOLVER:-127.0.0.11}|" /etc/nginx/conf.d/default.conf
echo "Proxy /api/ -> http://api:8080 (resolver ${RESOLVER:-127.0.0.11})"

exec nginx -g "daemon off;"
