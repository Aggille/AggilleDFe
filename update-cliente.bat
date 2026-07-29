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
REM
REM  Pre-requisito 2: os pacotes no GHCR (aggilledfe-api/worker/web) sao
REM  PRIVADOS - o "docker compose pull" abaixo so funciona se o servidor ja
REM  tiver feito "docker login ghcr.io" com um token valido. Se o pull falhar
REM  com "denied: denied" (token expirado/revogado ou nunca logado), conecte
REM  no servidor via SSH e rode, uma vez so, com um Personal Access Token do
REM  GitHub com escopo read:packages (pode ser o mesmo valor do secret
REM  GHCR_TOKEN do repositorio, que ja tem esse escopo):
REM
REM    ** RODAR DENTRO DO SERVIDOR CO CLIENTE
REM    docker login ghcr.io -u Aggille -p SEU_PERSONAL_ACCESS_TOKEN_AQUI
REM
REM  O login fica salvo no servidor (~/.docker/config.json do usuario root) -
REM  so precisa repetir se o token for revogado/expirar.
REM ============================================================================

set SERVIDOR=172.16.0.3
set USUARIO=root
set PASTA_REMOTA=/opt/aggilledfe/docker

echo.
echo Copiando docker-compose.prod.yml atualizado para %USUARIO%@%SERVIDOR% (vai pedir a senha)...
echo.

scp docker-compose.prod.yml %USUARIO%@%SERVIDOR%:%PASTA_REMOTA%/docker-compose.prod.yml
if errorlevel 1 (
    echo.
    echo [ERRO] Falha ao copiar docker-compose.prod.yml - veja a mensagem acima.
    pause
    exit /b 1
)

echo.
echo Conectando em %USUARIO%@%SERVIDOR% (vai pedir a senha de novo)...
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
