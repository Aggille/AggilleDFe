#!/bin/sh
set -e

# Aplica as migrations (bundle self-contained gerado no build da imagem, ver
# Dockerfile) antes de subir a API. Roda a cada start do container - o
# bundle das migrations do EF Core ja e idempotente (so aplica o que faltar).
if [ -n "$ConnectionStrings__DefaultConnection" ]; then
    echo "Aplicando migrations..."
    /app/efbundle --connection "$ConnectionStrings__DefaultConnection"
else
    echo "AVISO: ConnectionStrings__DefaultConnection nao definida - pulando migrations." >&2
fi

exec dotnet AggilleDFe.API.dll
