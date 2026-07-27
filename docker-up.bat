@echo off
REM ============================================================================
REM  docker-up.bat - Builda e sobe os containers do AggilleDFe (db, api,
REM  worker, web) via docker-compose.yml, a partir da raiz do repositorio.
REM  Ver DOCKER.md para detalhes (volumes de certificados/XMLs, migrations,
REM  variaveis de ambiente).
REM ============================================================================

where docker >nul 2>nul
if errorlevel 1 (
    echo [ERRO] docker nao encontrado no PATH. Instale o Docker Desktop.
    exit /b 1
)

if not exist ".env" (
    echo.
    echo [ERRO] Arquivo .env nao encontrado.
    echo         Copie .env.example para .env e preencha POSTGRES_PASSWORD
    echo         (e API_PUBLIC_URL/WEB_PUBLIC_URL, se necessario^) antes de rodar.
    echo.
    exit /b 1
)

echo.
echo Buildando e subindo os containers (db, api, worker, web)...
docker compose up --build -d
if errorlevel 1 (
    echo.
    echo [ERRO] "docker compose up" falhou - veja a mensagem acima.
    exit /b 1
)

echo.
echo ============================================================================
echo  Containers no ar.
echo    Web:  http://localhost:5071/   (ou o WEB_PUBLIC_URL do seu .env)
echo    API:  http://localhost:5007/swagger
echo.
echo  Comandos uteis:
echo    docker compose logs -f          (acompanhar os logs de todos)
echo    docker compose ps               (status dos containers)
echo    docker compose down             (parar e remover os containers)
echo ============================================================================
