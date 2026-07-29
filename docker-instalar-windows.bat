@echo off
setlocal EnableDelayedExpansion
REM ============================================================================
REM  docker-instalar-windows.bat - Sobe o AggilleDFe pela primeira vez numa
REM  maquina Windows com Docker ja instalado, usando as imagens prontas do
REM  GHCR (docker-compose.prod.yml) - nao builda nada localmente, nao precisa
REM  do codigo-fonte, so deste arquivo + docker-compose.prod.yml + .env na
REM  mesma pasta. Ver DOCKER.md ("Instalando o Docker em uma maquina Windows"
REM  e "Instalar em outra maquina via GHCR") para detalhes.
REM
REM  Pre-requisito: os pacotes do GHCR (aggilledfe-api/worker/web) sao
REM  PRIVADOS - e preciso logar com um Personal Access Token do GitHub (escopo
REM  read:packages) antes do primeiro pull. Este script pede usuario/token na
REM  primeira vez; se voce ja tiver feito "docker login ghcr.io" nesta maquina
REM  antes, pode responder "n" quando ele perguntar.
REM ============================================================================

where docker >nul 2>nul
if errorlevel 1 (
    echo [ERRO] docker nao encontrado no PATH. Instale o Docker Desktop.
    pause
    exit /b 1
)

if not exist "docker-compose.prod.yml" (
    echo.
    echo [ERRO] docker-compose.prod.yml nao encontrado nesta pasta.
    echo         Copie esse arquivo (da raiz do repositorio) pra cá antes de rodar.
    echo.
    pause
    exit /b 1
)

if not exist ".env" (
    echo.
    echo [ERRO] Arquivo .env nao encontrado.
    echo         Copie .env.example para .env e preencha POSTGRES_PASSWORD
    echo         (e API_PUBLIC_URL/WEB_PUBLIC_URL, se necessario^) antes de rodar.
    echo.
    pause
    exit /b 1
)

echo.
echo Os pacotes do GHCR sao privados - precisa estar logado pra baixar as imagens.
set /p FAZER_LOGIN="Fazer login no GHCR agora? (s/n) "
if /i "%FAZER_LOGIN%"=="s" (
    set /p GH_USUARIO="Usuario do GitHub: "
    echo Atencao: o "set /p" do cmd.exe mostra o que voce digita na tela (nao mascara).
    set /p GH_TOKEN="Personal Access Token (escopo read:packages): "
    echo !GH_TOKEN! | docker login ghcr.io -u !GH_USUARIO! --password-stdin
    if errorlevel 1 (
        echo.
        echo [ERRO] Login no GHCR falhou - veja a mensagem acima.
        pause
        exit /b 1
    )
)

echo.
echo Baixando as imagens do GHCR (api, worker, web)...
docker compose -f docker-compose.prod.yml pull
if errorlevel 1 (
    echo.
    echo [ERRO] "docker compose pull" falhou - veja a mensagem acima.
    echo         Se o erro for "denied: denied", o login no GHCR nao deu certo
    echo         ou o token nao tem o escopo read:packages - rode este script
    echo         de novo e responda "s" pro login.
    pause
    exit /b 1
)

echo.
echo Subindo os containers (db, api, worker, web)...
docker compose -f docker-compose.prod.yml up -d
if errorlevel 1 (
    echo.
    echo [ERRO] "docker compose up" falhou - veja a mensagem acima.
    pause
    exit /b 1
)

echo.
echo ============================================================================
echo  AggilleDFe no ar.
echo    Web:  http://localhost:5071/   (ou o WEB_PUBLIC_URL do seu .env)
echo    API:  http://localhost:5007/swagger
echo.
echo  Comandos uteis:
echo    docker compose -f docker-compose.prod.yml logs -f    (acompanhar os logs)
echo    docker compose -f docker-compose.prod.yml ps         (status dos containers)
echo    docker compose -f docker-compose.prod.yml down       (parar e remover)
echo.
echo  Pra atualizar depois de uma nova publicacao de imagens, use
echo  docker-atualizar-windows.bat.
echo ============================================================================
pause
