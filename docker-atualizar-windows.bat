@echo off
REM ============================================================================
REM  docker-atualizar-windows.bat - Atualiza o AggilleDFe numa maquina Windows
REM  que ja roda os containers via docker-compose.prod.yml (GHCR): puxa as
REM  imagens mais recentes, recria os containers e limpa imagens antigas.
REM  Rodar a partir da mesma pasta do docker-instalar-windows.bat.
REM
REM  Pre-requisito: as imagens novas ja precisam estar publicadas no GHCR
REM  (workflow do GitHub Actions, .github/workflows/docker-publish.yml, roda
REM  sozinho a cada push na main).
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
    echo         Copie a versao atualizada desse arquivo (da raiz do
    echo         repositorio) pra cá antes de rodar, se ele tiver mudado.
    echo.
    pause
    exit /b 1
)

echo.
echo Baixando as imagens mais recentes do GHCR...
docker compose -f docker-compose.prod.yml pull
if errorlevel 1 (
    echo.
    echo [ERRO] "docker compose pull" falhou - veja a mensagem acima.
    echo         Se o erro for "denied: denied", o login no GHCR expirou ou
    echo         foi revogado - rode:
    echo           docker login ghcr.io -u SEU_USUARIO_GITHUB
    echo         (vai pedir um Personal Access Token com escopo read:packages
    echo         no lugar da senha^) e tente de novo.
    pause
    exit /b 1
)

echo.
echo Recriando os containers com as imagens novas...
docker compose -f docker-compose.prod.yml up -d --force-recreate
if errorlevel 1 (
    echo.
    echo [ERRO] "docker compose up" falhou - veja a mensagem acima.
    pause
    exit /b 1
)

echo.
echo Limpando imagens antigas...
docker image prune -f

echo.
echo ============================================================================
echo  Atualizacao concluida.
echo    Web:  http://localhost:5071/   (ou o WEB_PUBLIC_URL do seu .env)
echo    API:  http://localhost:5007/swagger
echo ============================================================================
pause
