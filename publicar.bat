@echo off
REM ============================================================================
REM  publicar.bat - Builda as imagens (api, worker, web) a partir do codigo
REM  atual e publica no GHCR (ghcr.io/aggille/aggilledfe-*:latest).
REM
REM  Pre-requisitos:
REM    - Docker Desktop rodando
REM    - Login feito uma vez: docker login ghcr.io -u SEU_USUARIO
REM      (token classic com escopo write:packages)
REM
REM  Depois de rodar este script com sucesso, rode update-cliente.bat pra
REM  aplicar a imagem nova no servidor do cliente.
REM ============================================================================

where docker >nul 2>nul
if errorlevel 1 (
    echo [ERRO] docker nao encontrado no PATH. Instale/abra o Docker Desktop.
    exit /b 1
)

echo.
echo Buildando as imagens (api, worker, web) a partir do codigo atual...
docker compose build api worker web
if errorlevel 1 (
    echo.
    echo [ERRO] "docker compose build" falhou - veja a mensagem acima.
    pause
    exit /b 1
)

echo.
echo Publicando no GHCR (ghcr.io/aggille/aggilledfe-*:latest)...
docker compose push api worker web
if errorlevel 1 (
    echo.
    echo [ERRO] "docker compose push" falhou - veja a mensagem acima.
    echo         Se for erro de autenticacao, rode:
    echo         docker login ghcr.io -u SEU_USUARIO
    pause
    exit /b 1
)

echo.
echo ============================================================================
echo  Imagens publicadas com sucesso no GHCR.
echo  Proximo passo: rode update-cliente.bat para aplicar no servidor do cliente.
echo ============================================================================
pause
