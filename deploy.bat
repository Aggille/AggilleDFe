@echo off
REM ============================================================================
REM  deploy.bat - Publica AggilleDFe (API + Web + Worker) no servidor Linux
REM  172.16.0.3, via SSH. So cuida do deploy da APLICACAO (publish, copia dos
REM  arquivos, systemd units, config do Nginx) - o banco de dados JA EXISTE e
REM  nao e mexido por este script (nem criado, nem migrado).
REM  Roda a partir da raiz do repositorio, no Windows.
REM
REM  PRE-REQUISITOS NESTA MAQUINA (Windows):
REM    - .NET SDK 10 (dotnet publish)
REM    - Cliente OpenSSH (ssh.exe / scp.exe) - built-in no Windows 10/11
REM    - Chave SSH ja autorizada em root@172.16.0.3 (sem prompt de senha)
REM
REM  PRE-REQUISITOS NO SERVIDOR (172.16.0.3), conforme informado pelo usuario:
REM    - .NET 10 Runtime, PostgreSQL e Nginx JA INSTALADOS
REM    - Banco de dados aggilledfe JA CRIADO e com as migrations em dia
REM
REM  IMPORTANTE - REVISE ANTES DE RODAR:
REM    1. Preencha DB_CONNECTION_STRING abaixo com a connection string real do
REM       banco ja existente (o script recusa rodar com o valor padrao).
REM    2. Este script gera os arquivos de unit do systemd e a config do Nginx
REM       do zero - ele foi pensado para RODAR UMA VEZ, na primeira instalacao
REM       da aplicacao nesse servidor. Rodar de novo sobrescreve esses
REM       arquivos (o que e seguro para atualizar a aplicacao em si).
REM    3. Sem TLS/dominio: o IP 172.16.0.3 e privado (rede interna), entao o
REM       script serve tudo em HTTP puro, sem certificado. Se isso for
REM       exposto na internet depois, revise a secao 5 do DEPLOY_LINUX.md (TLS).
REM    4. O script NAO instala .NET/PostgreSQL/Nginx nem mexe no banco -
REM       assume que ja estao prontos no servidor, conforme confirmado.
REM ============================================================================

setlocal enabledelayedexpansion

REM ---- Variaveis de configuracao (edite antes de rodar) ----------------------
set SERVER=172.16.0.3
set SSH_USER=root
set SSH_TARGET=%SSH_USER%@%SERVER%
set REMOTE_BASE=/opt/aggilledfe
set API_PORT=5007
set WEB_PORT=5071
set SERVICE_USER=aggilledfe

REM Connection string COMPLETA do banco ja existente (host pode ser 127.0.0.1
REM se o Postgres roda no mesmo servidor, ou outro IP se for um banco a parte).
REM NAO coloque aspas ao redor do valor - "set VAR=valor" nao remove aspas
REM como em outras linguagens, elas ficariam dentro do valor e quebram o
REM script mais adiante (erro classico: "=<algo> foi inesperado neste momento").
set DB_CONNECTION_STRING=Host=172.16.0.3;Port=5432;Database=aggilledfe;Username=aggilledfe;Password=Ag1ll32017

set LOCAL_PUBLISH=.\publish
set LOCAL_DEPLOY_CFG=.\publish\deploy-config

REM ---- Guarda de seguranca: nao deixa rodar sem preencher a connection string
if "%DB_CONNECTION_STRING%"=="TROQUE_ESTA_CONNECTION_STRING" (
    echo.
    echo [ERRO] Edite DB_CONNECTION_STRING no topo deste script antes de rodar.
    echo.
    exit /b 1
)
if not "%DB_CONNECTION_STRING%"=="%DB_CONNECTION_STRING:"=%" (
    echo.
    echo [ERRO] DB_CONNECTION_STRING nao pode ter aspas dentro do valor. Remova-as.
    echo.
    exit /b 1
)

where ssh >nul 2>nul
if errorlevel 1 (
    echo [ERRO] ssh.exe nao encontrado no PATH. Instale o "Cliente OpenSSH" do Windows.
    exit /b 1
)
where scp >nul 2>nul
if errorlevel 1 (
    echo [ERRO] scp.exe nao encontrado no PATH. Instale o "Cliente OpenSSH" do Windows.
    exit /b 1
)

echo.
echo ============================================================================
echo  Publicando em %SSH_TARGET%:%REMOTE_BASE% - CTRL+C agora para cancelar
echo ============================================================================
timeout /t 5

REM ==== 1. Build e publish (API, Web, Worker) =================================
echo.
echo [1/6] dotnet publish (API / Web / Worker)...
if exist "%LOCAL_PUBLISH%" rmdir /s /q "%LOCAL_PUBLISH%"
mkdir "%LOCAL_DEPLOY_CFG%"

dotnet publish AggilleDFe.API\AggilleDFe.API.csproj       -c Release -o "%LOCAL_PUBLISH%\api"
if errorlevel 1 goto :erro
dotnet publish AggilleDFe.Web\AggilleDFe.Web.csproj       -c Release -o "%LOCAL_PUBLISH%\web"
if errorlevel 1 goto :erro
dotnet publish AggilleDFe.Worker\AggilleDFe.Worker.csproj -c Release -o "%LOCAL_PUBLISH%\worker"
if errorlevel 1 goto :erro

REM ==== 2. appsettings.Production.json (API e Worker) com a connection string =
echo.
echo [2/6] Gerando appsettings.Production.json (API / Worker)...
(
    echo {
    echo   "ConnectionStrings": {
    echo     "DefaultConnection": "%DB_CONNECTION_STRING%"
    echo   },
    echo   "WebClientOrigins": [ "http://%SERVER%:%WEB_PORT%" ],
    echo   "SchemasPath": "SCHEMAS"
    echo }
) > "%LOCAL_DEPLOY_CFG%\appsettings.Production.api.json"

(
    echo {
    echo   "ConnectionStrings": {
    echo     "DefaultConnection": "%DB_CONNECTION_STRING%"
    echo   }
    echo }
) > "%LOCAL_DEPLOY_CFG%\appsettings.Production.worker.json"

REM Aponta o Web publicado para a API real do servidor
(
    echo { "ApiUrl": "http://%SERVER%:%API_PORT%" }
) > "%LOCAL_DEPLOY_CFG%\web-appsettings.json"

REM ==== 3. Units do systemd (API e Worker) =====================================
echo.
echo [3/6] Gerando units do systemd...
(
    echo [Unit]
    echo Description=AggilleDFe API
    echo After=network.target postgresql.service
    echo.
    echo [Service]
    echo WorkingDirectory=%REMOTE_BASE%/api
    echo ExecStart=/usr/bin/dotnet %REMOTE_BASE%/api/AggilleDFe.API.dll
    echo Restart=always
    echo RestartSec=10
    echo User=%SERVICE_USER%
    echo Environment=ASPNETCORE_ENVIRONMENT=Production
    echo Environment=ASPNETCORE_URLS=http://0.0.0.0:%API_PORT%
    echo.
    echo [Install]
    echo WantedBy=multi-user.target
) > "%LOCAL_DEPLOY_CFG%\aggilledfe-api.service"

(
    echo [Unit]
    echo Description=AggilleDFe Worker
    echo After=network.target postgresql.service
    echo.
    echo [Service]
    echo WorkingDirectory=%REMOTE_BASE%/worker
    echo ExecStart=/usr/bin/dotnet %REMOTE_BASE%/worker/AggilleDFe.Worker.dll
    echo Restart=always
    echo RestartSec=10
    echo User=%SERVICE_USER%
    echo Environment=DOTNET_ENVIRONMENT=Production
    echo.
    echo [Install]
    echo WantedBy=multi-user.target
) > "%LOCAL_DEPLOY_CFG%\aggilledfe-worker.service"

REM ==== 4. Config do Nginx (serve o Web estatico em HTTP, porta 5071) =========
echo.
echo [4/6] Gerando config do Nginx...
(
    echo server {
    echo     listen %WEB_PORT%;
    echo     server_name %SERVER%;
    echo.
    echo     root %REMOTE_BASE%/web/wwwroot;
    echo     index index.html;
    echo.
    echo     location / {
    echo         try_files $uri $uri/ /index.html;
    echo     }
    echo.
    echo     location ~ \.wasm$ { types { application/wasm wasm; } }
    echo     gzip_static on;
    echo }
) > "%LOCAL_DEPLOY_CFG%\aggilledfe-web.nginx.conf"

REM ==== 5. Prepara pastas remotas + copia os arquivos ==========================
echo.
echo [5/6] Preparando pastas remotas e copiando arquivos (pode demorar)...

ssh %SSH_TARGET% "id -u %SERVICE_USER% >/dev/null 2>&1 || useradd --system --no-create-home --shell /usr/sbin/nologin %SERVICE_USER%"
if errorlevel 1 goto :erro

ssh %SSH_TARGET% "mkdir -p %REMOTE_BASE%/api/SCHEMAS %REMOTE_BASE%/web/wwwroot %REMOTE_BASE%/worker"
if errorlevel 1 goto :erro

scp -r "%LOCAL_PUBLISH%\api\*" %SSH_TARGET%:%REMOTE_BASE%/api/
if errorlevel 1 goto :erro
scp -r "%LOCAL_PUBLISH%\web\wwwroot\*" %SSH_TARGET%:%REMOTE_BASE%/web/wwwroot/
if errorlevel 1 goto :erro
scp -r "%LOCAL_PUBLISH%\worker\*" %SSH_TARGET%:%REMOTE_BASE%/worker/
if errorlevel 1 goto :erro

scp "%LOCAL_DEPLOY_CFG%\appsettings.Production.api.json"    %SSH_TARGET%:%REMOTE_BASE%/api/appsettings.Production.json
if errorlevel 1 goto :erro
scp "%LOCAL_DEPLOY_CFG%\appsettings.Production.worker.json" %SSH_TARGET%:%REMOTE_BASE%/worker/appsettings.Production.json
if errorlevel 1 goto :erro
scp "%LOCAL_DEPLOY_CFG%\web-appsettings.json" %SSH_TARGET%:%REMOTE_BASE%/web/wwwroot/appsettings.json
if errorlevel 1 goto :erro

scp "%LOCAL_DEPLOY_CFG%\aggilledfe-api.service"    %SSH_TARGET%:/etc/systemd/system/aggilledfe-api.service
if errorlevel 1 goto :erro
scp "%LOCAL_DEPLOY_CFG%\aggilledfe-worker.service" %SSH_TARGET%:/etc/systemd/system/aggilledfe-worker.service
if errorlevel 1 goto :erro
scp "%LOCAL_DEPLOY_CFG%\aggilledfe-web.nginx.conf" %SSH_TARGET%:/etc/nginx/sites-available/aggilledfe
if errorlevel 1 goto :erro

REM ==== 6. Ajusta permissoes, ativa o Nginx e os servicos ======================
echo.
echo [6/6] Ativando Nginx e servicos...

ssh %SSH_TARGET% "chown -R %SERVICE_USER%:%SERVICE_USER% %REMOTE_BASE%/api %REMOTE_BASE%/worker"
if errorlevel 1 goto :erro

ssh %SSH_TARGET% "ln -sf /etc/nginx/sites-available/aggilledfe /etc/nginx/sites-enabled/aggilledfe && nginx -t && systemctl reload nginx"
if errorlevel 1 goto :erro

ssh %SSH_TARGET% "systemctl daemon-reload && systemctl enable --now aggilledfe-api aggilledfe-worker && systemctl restart aggilledfe-api aggilledfe-worker"
if errorlevel 1 goto :erro

echo.
echo ============================================================================
echo  Deploy concluido.
echo    Web:  http://%SERVER%:%WEB_PORT%/
echo    API:  http://%SERVER%:%API_PORT%/swagger
echo.
echo  Falta fazer manualmente (nao automatizado por este script):
echo    - Garantir que o banco aggilledfe ja tem as migrations em dia (ver
echo      DEPLOY_LINUX.md secao 3 - "dotnet ef database update" ou script SQL).
echo    - Copiar os schemas XSD do Zeus DFe.NET para %REMOTE_BASE%/api/SCHEMAS
echo      (ver DEPLOY_LINUX.md secao 6) - opcional, mas recomendado.
echo    - Copiar os certificados .pfx de cada empresa para o servidor e
echo      cadastrar o caminho completo na tela de Empresas.
echo    - Conferir se a porta %API_PORT% e a porta %WEB_PORT% estao liberadas no
echo      firewall do servidor (ufw/firewalld), se houver algum ativo.
echo    - Se este servidor for exposto fora da rede interna, configurar TLS
echo      (ver DEPLOY_LINUX.md secao 5) - hoje o Nginx serve so HTTP puro.
echo ============================================================================
goto :fim

:erro
echo.
echo [ERRO] O deploy parou no passo acima. Corrija o problema e rode de novo.
exit /b 1

:fim
endlocal
