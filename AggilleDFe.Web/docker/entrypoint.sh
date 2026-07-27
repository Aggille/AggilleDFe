#!/bin/sh
set -e

# O Blazor WASM le a URL da API de wwwroot/appsettings.json em tempo de
# execucao (fetch, no navegador do usuario) - por isso da pra trocar so com
# uma variavel de ambiente do container, sem precisar recompilar o Web.
API_URL="${API_URL:-http://localhost:5007}"

cat > /usr/share/nginx/html/appsettings.json <<EOF
{ "ApiUrl": "$API_URL" }
EOF

exec nginx -g "daemon off;"
