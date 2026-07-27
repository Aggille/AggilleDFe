#!/bin/sh
set -e

# O Blazor WASM le a URL da API de wwwroot/appsettings.json em tempo de
# execucao (fetch, no navegador do usuario) - por isso da pra trocar so com
# uma variavel de ambiente do container, sem precisar recompilar o Web.
API_URL="${API_URL:-http://localhost:5007}"

cat > /usr/share/nginx/html/appsettings.json <<EOF
{ "ApiUrl": "$API_URL" }
EOF

# Remove variantes pre-comprimidas geradas pelo "dotnet publish" com o valor
# original do wwwroot - com gzip_static on no nginx.conf, o nginx serviria
# esse .gz/.br antigo (com o ApiUrl de build) em vez do arquivo acima recem
# reescrito, ja que o navegador sempre aceita gzip.
rm -f /usr/share/nginx/html/appsettings.json.gz /usr/share/nginx/html/appsettings.json.br

exec nginx -g "daemon off;"
