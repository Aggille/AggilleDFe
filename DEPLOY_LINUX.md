# Manual de Deploy e Configuração — AggilleDFe (Linux)

Guia para colocar a plataforma AggilleDFe em produção em um servidor
**Linux**. Cobre os três componentes que rodam continuamente:

- **AggilleDFe.API** — API ASP.NET Core (minimal APIs), expõe os endpoints
  REST e o Swagger (`/swagger`)
- **AggilleDFe.Web** — front-end Blazor WebAssembly **standalone** (sem
  servidor próprio — publica como arquivos estáticos)
- **AggilleDFe.Worker** — serviço de background para o ciclo agendado de
  download de XMLs

`AggilleDFe.Maui` é o app companion e não faz parte deste guia de deploy de
servidor. Para deploy em Windows, ver `DEPLOY_WINDOWS.md`. Para publicar
automaticamente via SSH, ver o script `deploy.bat` na raiz do repositório
(roda a partir de uma máquina Windows de desenvolvimento, publica e copia
tudo para um servidor Linux de destino).

---

## 1. Pré-requisitos

| Item | Versão / detalhe |
|---|---|
| .NET SDK/Runtime | .NET 10 (`dotnet --version` deve reportar 10.x). Em produção, o **ASP.NET Core Runtime** 10 basta para API/Worker; para publicar o Web também é necessário o SDK completo (pode ser publicado em outra máquina e só copiar o resultado — é o que o `deploy.bat` faz, publicando no Windows e enviando por SSH). |
| Banco de dados | PostgreSQL (qualquer versão recente com suporte a `time without time zone`). Nome de banco convencionado: `aggilledfe`. |
| Certificados digitais A1 (.pfx) | Um por empresa cadastrada, acessíveis pelo caminho de arquivo configurado no cadastro da empresa. |
| Schemas XSD do Zeus DFe.NET | Pasta plana de `.xsd` (ver seção 6) — opcional, mas recomendada. |
| Nginx | Necessário para hospedar o `AggilleDFe.Web` (arquivos estáticos do Blazor WASM). |

---

## 2. Build e publish

Os três componentes (API, Web, Worker) são publicados separadamente — cada
`dotnet publish` monta a árvore de arquivos de UM projeto — mas com `-o`
apontando para subpastas de uma única pasta de saída, tudo fica organizado
em um só lugar (`./publish`), pronto para ser copiado/zipado inteiro para o
servidor de destino.

A partir da raiz do repositório (bash — funciona tanto rodando direto no
servidor Linux quanto numa máquina Linux/macOS de build):

```bash
rm -rf ./publish
dotnet publish AggilleDFe.API/AggilleDFe.API.csproj       -c Release -o ./publish/api
dotnet publish AggilleDFe.Web/AggilleDFe.Web.csproj       -c Release -o ./publish/web
dotnet publish AggilleDFe.Worker/AggilleDFe.Worker.csproj -c Release -o ./publish/worker
```

Resultado:

```
publish/
├── api/          # AggilleDFe.API.dll + dependências (roda com "dotnet AggilleDFe.API.dll")
├── web/
│   └── wwwroot/  # arquivos estáticos do Blazor WASM — isso aqui é o que vai pro Nginx
└── worker/       # AggilleDFe.Worker.dll + dependências
```

**Não** use `dotnet publish AggilleDFe.slnx` (publicando a `.slnx` inteira de
uma vez) — o projeto `AggilleDFe.Maui` é multi-target (`net10.0-android`,
`-ios`, `-maccatalyst`, `-windows...`) e o `publish` de solução falha nele
com `NETSDK1129` ("O destino 'Publish' não é suportado sem a especificação
de uma estrutura de destino"), já que não há como o comando saber sozinho
qual dos quatro frameworks do MAUI publicar. Os três comandos separados por
projeto acima são a forma confiável (e são os únicos necessários para o
deploy de servidor — o MAUI é o app companion, publicado à parte quando for
o caso, com `-f <framework>` explícito).

Depois de publicado, compacte a pasta inteira para transferir ao servidor
(se não estiver usando `deploy.bat`, que já copia via `scp` direto):

```bash
tar -czf aggilledfe-publish.tar.gz -C publish .
```

Os binários publicados são "framework-dependent" (o padrão) — exigem o
ASP.NET Core Runtime 10 instalado na máquina de destino. Não é necessário
compilar em Linux para rodar em Linux — o .NET SDK é multiplataforma;
publicar no Windows e copiar o resultado (via `scp`, por exemplo) funciona
normalmente, como o script `deploy.bat` faz.

---

## 3. Banco de dados

1. Criar o banco `aggilledfe` no PostgreSQL (usuário/senha à sua escolha).
2. Aplicar as migrations. Duas opções:
   - **Via `dotnet ef`** (precisa do SDK e do código-fonte na máquina que
     roda o comando — pode ser a máquina de deploy ou uma máquina de build,
     apontando para o banco de produção via connection string):
     ```bash
     dotnet tool install --global dotnet-ef   # se ainda não tiver
     dotnet ef database update --project AggilleDFe.Infrastructure --startup-project AggilleDFe.API
     ```
   - **Via script SQL gerado offline** (não precisa do SDK na máquina do
     banco):
     ```bash
     dotnet ef migrations script --project AggilleDFe.Infrastructure --startup-project AggilleDFe.API -o migrar.sql
     ```
     e depois aplicar `migrar.sql` com `psql` ou outra ferramenta.
3. A connection string de produção **nunca** deve ir para o `appsettings.json`
   commitado (que é mantido vazio de propósito). Configure via:
   - `appsettings.Production.json` (arquivo separado, fora do controle de
     versão, na pasta de publish), ou
   - variável de ambiente `ConnectionStrings__DefaultConnection` (dois
     underscores — é assim que o ASP.NET Core mapeia variáveis de ambiente
     para configuração hierárquica), ou
   - `dotnet user-secrets` (só faz sentido em dev).

Exemplo de variável de ambiente:
```bash
export ConnectionStrings__DefaultConnection="Host=meuservidor;Port=5432;Database=aggilledfe;Username=aggilledfe;Password=SENHA_REAL"
```

---

## 4. Configuração da API (`appsettings.json` / variáveis de ambiente)

Chaves relevantes (todas em `AggilleDFe.API/appsettings.json`, sobrescrevíveis
por `appsettings.Production.json` ou variáveis de ambiente):

| Chave | Padrão | Descrição |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | vazio | String de conexão PostgreSQL (ver seção 3) |
| `WebClientOrigins` | `["http://localhost:5071", "https://localhost:7170"]` | Lista de origens permitidas no CORS. **Precisa ser trocada em produção** para a(s) URL(s) real(is) onde o `AggilleDFe.Web` publicado vai ficar hospedado (ex.: `["https://dfe.suaempresa.com.br"]`), senão o navegador bloqueia as chamadas da tela para a API. |
| `SchemasPath` | `SCHEMAS` (relativo à pasta de trabalho do processo) | Pasta com os `.xsd` do Zeus DFe.NET (ver seção 6). Se a pasta não existir, a validação local de schema é simplesmente desativada — a aplicação continua funcionando. |

Não existe `CertificadosPath`/upload de certificado — desde que a API roda
com as permissões da conta do sistema operacional (não é sandboxed no
navegador), cada empresa cadastrada informa o **caminho completo** do
arquivo `.pfx` já existente no servidor onde a API roda (campo "Caminho do
Certificado Digital" no cadastro da empresa). Garanta que a conta que executa
o processo da API (o `User=` da unit do systemd, seção 7) tenha permissão de
**leitura** nesses caminhos.

O mesmo vale para a "Pasta de XMLs" de cada empresa — é o caminho completo
onde o Worker vai gravar os XMLs baixados; a conta do processo precisa de
permissão de **escrita** ali.

---

## 5. Portas e HTTPS

Em desenvolvimento o projeto roda propositalmente em HTTP puro
(`ASPNETCORE_URLS=http://localhost:5007`) — expor HTTP e HTTPS ao mesmo
tempo no Kestrel de dev causava um bug de upgrade automático para HTTPS no
navegador que quebrava as chamadas da API (`Failed to fetch`).

Em produção, o padrão recomendado é **não** terminar TLS no Kestrel
diretamente: coloque a API atrás de um reverse proxy (Nginx/Caddy) que
termina o HTTPS e repassa HTTP puro para o Kestrel internamente. Configure
`ASPNETCORE_URLS` para escutar só em `http://127.0.0.1:<porta interna>`
(ex.: `5007`), e deixe o reverse proxy expor a porta 443 pública com
certificado real.

Se o servidor estiver só em rede interna (IP privado, sem domínio público),
não há TLS/Let's Encrypt possível — nesse caso é comum servir tudo em HTTP
puro mesmo (é o que o `deploy.bat` faz, pensado para esse cenário).

Se preferir terminar TLS direto no Kestrel (sem reverse proxy), configure em
`appsettings.Production.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:5007",
        "Certificate": { "Path": "/caminho/para/cert.pfx", "Password": "..." }
      }
    }
  }
}
```

---

## 6. Pasta de Schemas do Zeus DFe.NET (opcional, recomendada)

O Zeus DFe.NET valida XML localmente contra XSDs quando eles estão
disponíveis (ver `AggilleDFe.Infrastructure/Integrations/ZEUS_CONFIGURACAO.md`
para o detalhamento técnico). Os arquivos não vêm no pacote NuGet.

1. Copie os `.xsd` (estrutura **plana**, sem subpastas — ex.:
   `consStatServ_v4.00.xsd`) para a pasta apontada por `SchemasPath`
   (padrão: `SCHEMAS` dentro da pasta onde a API roda).
2. Fonte oficial: Portal Nacional da NF-e (nfe.fazenda.gov.br, "Documentos
   Técnicos"/"Schemas XML"). Para começar rápido em ambiente de
   desenvolvimento/homologação, os schemas do projeto de demonstração do
   próprio Zeus DFe.NET no GitHub (`ZeusAutomacao/DFe.NET`, pasta
   `NFe.AppTeste/Schemas`) já têm a estrutura correta.
3. Sem essa pasta, a aplicação funciona normalmente — só sem validação local
   antes de enviar ao SEFAZ.

---

## 7. Rodando como serviço (systemd)

Crie uma unit para a API (`/etc/systemd/system/aggilledfe-api.service`):

```ini
[Unit]
Description=AggilleDFe API
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/aggilledfe/api
ExecStart=/usr/bin/dotnet /opt/aggilledfe/api/AggilleDFe.API.dll
Restart=always
RestartSec=10
User=aggilledfe
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5007
EnvironmentFile=/opt/aggilledfe/api/.env

[Install]
WantedBy=multi-user.target
```

`.env` (fora do git, permissões restritas) contendo:
```
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=aggilledfe;Username=...;Password=...
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now aggilledfe-api
```

Mesma estrutura para `aggilledfe-worker.service` apontando para
`AggilleDFe.Worker.dll` (troque `ASPNETCORE_ENVIRONMENT` por
`DOTNET_ENVIRONMENT`, já que o Worker usa o Generic Host, não o Web Host).

Para o `AggilleDFe.Web`, sirva `publish/web/wwwroot` com **Nginx**:

```nginx
server {
    listen 443 ssl;
    server_name dfe.suaempresa.com.br;

    root /opt/aggilledfe/web/wwwroot;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;  # fallback de SPA
    }

    # Blazor publica variantes pré-comprimidas
    location ~ \.wasm$ { types { application/wasm wasm; } }
    gzip_static on;
    brotli_static on;   # se o módulo brotli estiver disponível no Nginx

    ssl_certificate     /etc/letsencrypt/live/dfe.suaempresa.com.br/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/dfe.suaempresa.com.br/privkey.pem;
}
```

Sem domínio público (rede interna, IP privado), troque por `listen 5071;` sem
bloco `ssl_certificate` — é o padrão usado pelo `deploy.bat` (Web na porta
5071, API na porta 5007).

Antes de publicar, edite `wwwroot/appsettings.json` (`ApiUrl`) para a URL
pública da API.

---

## 8. Docker

Implementado — ver **[DOCKER.md](DOCKER.md)** na raiz do repositório.
`docker-compose.yml` sobe os 4 serviços (PostgreSQL, API, Worker, Web) com
um `Dockerfile` multi-stage por componente; `docker-up.bat` automatiza o
`docker compose up --build` a partir do Windows. Já cobre o ajuste de
OpenSSL da seção 9 (legacy provider) dentro das imagens de API/Worker.

---

## 9. Observação sobre certificados A1 (.pfx)

Certificados A1 emitidos por ACs da ICP-Brasil frequentemente usam
algoritmos de criptografia legados (RC2-40-CBC/3DES) que o OpenSSL 3.0 —
padrão em distros Linux recentes — rejeita por padrão. Se o carregamento do
certificado falhar só no Linux (e funcionar no Windows), habilite o
"legacy provider" no `openssl.cnf` da máquina/imagem:

```ini
openssl_conf = openssl_init

[openssl_init]
providers = provider_sect

[provider_sect]
default = default_sect
legacy = legacy_sect

[default_sect]
activate = 1

[legacy_sect]
activate = 1
```

Isso não é específico do Zeus DFe.NET — afeta qualquer stack (.NET, Java,
Python) carregando esse tipo de certificado em Linux moderno.

---

## 10. Checklist de deploy

- [ ] Banco `aggilledfe` criado e migrations aplicadas
- [ ] `ConnectionStrings:DefaultConnection` configurada (fora do git)
- [ ] `WebClientOrigins` apontando para a URL real do Web publicado
- [ ] Pasta `SchemasPath` com os `.xsd` copiados (opcional, mas recomendado)
- [ ] Caminhos de certificado (`.pfx`) e pasta de XMLs de cada empresa
      acessíveis (leitura/escrita) pela conta que roda o processo da API
- [ ] `wwwroot/appsettings.json` do Web publicado com `ApiUrl` correto
- [ ] TLS configurado (reverse proxy), ou HTTP puro assumido conscientemente
      (rede interna)
- [ ] Serviço systemd (`aggilledfe-api`/`aggilledfe-worker`) habilitado com
      restart automático
- [ ] Site do Web servindo os arquivos estáticos publicados no Nginx
