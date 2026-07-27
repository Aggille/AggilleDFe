@echo off
REM ============================================================================
REM  update-cliente.bat - Atualiza o AggilleDFe no servidor Linux do cliente
REM  (172.16.0.3) via SSH: puxa as imagens mais recentes do GHCR, recria os
REM  containers e limpa imagens antigas. Pede a senha do root a cada execucao
REM  (sem chave configurada).
REM
REM  Pre-requisito: as imagens novas ja precisam estar publicadas no GHCR
REM  (ver DOCKER.md - "docker compose build" + "docker compose push", ou
REM  aguardar o workflow do GitHub Actions).
REM ============================================================================

set SERVIDOR=172.16.0.3
set USUARIO=root
set PASTA_REMOTA=/opt/aggilledfe/docker

echo.
echo Conectando em %USUARIO%@%SERVIDOR% (vai pedir a senha)...
echo Atualizando AggilleDFe em %PASTA_REMOTA%...
echo.

ssh %USUARIO%@%SERVIDOR% "cd %PASTA_REMOTA% && docker compose -f docker-compose.prod.yml pull && docker compose -f docker-compose.prod.yml up -d --force-recreate && docker image prune -f"

if errorlevel 1 (
    echo.
    echo [ERRO] A atualizacao falhou - veja a mensagem acima.
    pause
    exit /b 1
)

echo.
echo ============================================================================
echo  Atualizacao concluida.
echo    Web:  http://%SERVIDOR%:5071/
echo    API:  http://%SERVIDOR%:5007/swagger
echo ============================================================================
pause
