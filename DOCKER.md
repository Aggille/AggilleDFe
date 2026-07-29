# Docker (AggilleDFe)

Suporte a Docker via `docker-compose.yml` na raiz do repositório — sobe os
4 serviços: `db` (PostgreSQL), `api`, `worker` e `web`, cada um com seu
próprio `Dockerfile` (build multi-stage: SDK do .NET pra compilar/publicar,
depois uma imagem final enxuta em runtime).

## Passo a passo

1. Copie `.env.example` para `.env` e preencha:
   - `POSTGRES_PASSWORD` (obrigatório — o compose recusa subir sem isso)
   - `API_PUBLIC_URL` / `WEB_PUBLIC_URL` (URLs alcançáveis de fora do
     compose — pelo navegador do usuário, não a rede interna dos
     containers; ver comentários no próprio `.env.example`)
2. `docker compose up --build -d` (ou use `docker-up.bat` no Windows, que
   faz essa chamada e confere se o `.env` existe antes).
3. Acesse `http://localhost:5071` (Web) — a API fica em
   `http://localhost:5007/swagger`.

Na primeira subida, o container `db` começa **vazio**; o container `api`
aplica as migrations sozinho, todo start (via `docker-entrypoint.sh` +
`efbundle`, ver seção abaixo) — não precisa rodar `dotnet ef database
update` manualmente.

## Instalando o Docker em uma máquina Windows

Os passos acima (`docker compose up`) valem igual no Windows — só a
instalação do próprio Docker e os caminhos de volume mudam.

1. **Requisitos**: Windows 10 64-bit (build 19041+) ou Windows 11,
   virtualização habilitada na BIOS/UEFI (Intel VT-x/AMD-V — em geral já vem
   ligada; se o WSL reclamar de virtualização desabilitada, é aqui que se
   mexe).
2. **Instalar o WSL2** (PowerShell **como Administrador**):
   ```powershell
   wsl --install
   ```
   Reinicie a máquina quando pedir. Se o WSL já estiver instalado, só
   confirme a versão 2: `wsl --set-default-version 2`.
3. **Instalar o Docker Desktop**: baixe em
   https://www.docker.com/products/docker-desktop/ e rode o instalador.
   Na tela de opções, deixe marcado **"Use WSL 2 based engine"** (padrão
   nas versões atuais).
4. Abra o **Docker Desktop** e espere o ícone da baleia (bandeja do
   Windows) ficar parado/"Engine running" — os comandos `docker` não
   funcionam com o Desktop fechado ou ainda inicializando.
5. Confirme no PowerShell:
   ```powershell
   docker --version
   docker compose version
   ```
6. **Ajuste os volumes antes de subir**: o `docker-compose.yml` do repo traz
   caminhos de host em formato Linux (`/home/dados`,
   `/home/dados/Certificados`, `/home/dados/jels/NFE`, `/home/dados/jels` —
   ver seção "Certificados digitais e pasta de XMLs" abaixo). Isso é o
   padrão pensado pra servidor Linux; num host Windows, troque o lado
   **esquerdo** de cada linha em `volumes:` (serviços `api` e `worker`) para
   um caminho Windows, por exemplo:
   ```yaml
   volumes:
     - C:/aggilledfe/dados:/dados
     - C:/aggilledfe/dados/Certificados:/certificados
     - C:/aggilledfe/jels/NFE:/nfe
     - C:/aggilledfe/jels:/jels
   ```
   Use barra normal (`/`) mesmo no Windows — é o formato que o Docker
   Desktop espera dentro do compose. O lado **direito** (`/dados`,
   `/certificados`, `/nfe`, `/jels`, dentro do container) não muda.
7. **Compartilhamento de unidade**: se a pasta escolhida no passo 6 estiver
   fora de `C:\Users\...`, confirme em Docker Desktop → *Settings* →
   *Resources* → *File sharing* que o drive correspondente (ex.: `C:`) está
   liberado — versões mais novas do Docker Desktop compartilham tudo por
   padrão via WSL2, mas vale conferir se o container não subir enxergando a
   pasta vazia.
8. Rode a partir da raiz do repositório (PowerShell ou `cmd`):
   ```powershell
   docker-up.bat
   ```
   (ou `docker compose up --build -d` diretamente — `docker-up.bat` só
   confere se o `.env` existe antes de chamar o mesmo comando).

**Gotcha de fim de linha (CRLF) nos scripts**: `AggilleDFe.API/docker-entrypoint.sh`
e `AggilleDFe.Web/docker/entrypoint.sh` são scripts `sh` que rodam **dentro**
dos containers Linux. Se o Git no Windows estiver configurado com
`core.autocrlf=true` (comum em instalações padrão), esses arquivos podem ser
baixados com quebra de linha `CRLF`, e o container falha ao subir com erro
tipo `exec ./docker-entrypoint.sh: no such file or directory` (o shell do
Linux não reconhece o `\r` do fim de cada linha). Se acontecer:
- Confira com `git config --get core.autocrlf`.
- Se for `true`, reconfigure para este repositório
  (`git config core.autocrlf input`) e refaça o checkout desses dois
  arquivos (`git rm --cached AggilleDFe.API/docker-entrypoint.sh
  AggilleDFe.Web/docker/entrypoint.sh && git checkout --
  AggilleDFe.API/docker-entrypoint.sh AggilleDFe.Web/docker/entrypoint.sh`),
  ou simplesmente rode `dos2unix` nesses dois arquivos antes do `docker
  compose up --build`.

## Certificados digitais e pasta de XMLs

Os serviços `api` e `worker` montam **quatro pastas físicas do servidor**,
cada uma com seu próprio ponto de montagem dentro dos containers:

```yaml
volumes:
  - /home/dados:/dados
  - /home/dados/Certificados:/certificados
  - /home/dados/jels/NFE:/nfe
  - /home/dados/jels:/jels
```

| Pasta no servidor | Caminho dentro do container | Uso |
|---|---|---|
| `/home/dados` | `/dados` | Geral (schemas do Zeus DFe.NET, etc.) |
| `/home/dados/Certificados` | `/certificados` | Certificados digitais (`.pfx`) |
| `/home/dados/jels/NFE` | `/nfe` | Pasta de XMLs (estrutura já existente do JELS) |
| `/home/dados/jels` | `/jels` | Pasta do JELS inteira (pai de `NFE`, caso haja outras subpastas do JELS além dela) |

`/nfe` e `/jels` se sobrepõem de propósito (`/jels/NFE` enxerga os mesmos
arquivos que `/nfe`) — mantidos os dois pontos de montagem porque foram
pedidos separadamente; use o que for mais conveniente ao cadastrar caminhos
na tela de Empresas.

Os caminhos cadastrados na tela de Empresas **precisam usar o prefixo de
dentro do container**, não o caminho real no servidor — o container não
enxerga o sistema de arquivos do host além do que está montado como
volume. Exemplos:
- Certificado em `/home/dados/Certificados/empresa1.pfx` no servidor →
  cadastre `/certificados/empresa1.pfx` na tela de Empresas.
- Pasta de XMLs → cadastre `/nfe/...` (não `/home/dados/jels/NFE/...`).

`SchemasPath` da API aponta para `/dados/schemas` por padrão no
`docker-compose.yml` (ajuste lá se preferir outro caminho/subpasta).

Se os caminhos reais no seu servidor forem diferentes dos usados aqui
(`/home/dados`, `/home/dados/Certificados`, `/home/dados/jels/NFE`), ajuste
o lado esquerdo (host) de cada linha em `volumes:` nos serviços `api` e
`worker` do `docker-compose.yml` — o lado direito (`/dados`,
`/certificados`, `/nfe`) pode manter, é só o que fica visível de dentro do
container.

## Migrations via EF Core bundle (self-contained)

`AggilleDFe.API/Dockerfile` gera um `efbundle` (executável self-contained
do EF Core, `dotnet ef migrations bundle --self-contained -r linux-x64`) no
estágio de build — não precisa do SDK/dotnet-ef na imagem final. O
`docker-entrypoint.sh` roda esse bundle contra
`ConnectionStrings__DefaultConnection` antes de subir a API; o bundle já é
idempotente (só aplica as migrations que faltam), então rodar de novo em
containers já migrados não faz nada.

## CORS e URL da API no Web (Blazor WASM)

O `AggilleDFe.Web` publica como arquivos estáticos (servidos por
`nginx:alpine`) — o `ApiUrl` que o Blazor usa pra chamar a API é lido de
`wwwroot/appsettings.json` **em tempo de execução no navegador**, não em
tempo de build. Por isso o container `web` tem um `entrypoint.sh` que
reescreve esse arquivo com a variável de ambiente `API_URL` a cada start,
antes de subir o Nginx — dá pra trocar a URL da API só mudando a variável
de ambiente do container `web`, sem recompilar nada.

A API, por sua vez, precisa liberar essa origem no CORS
(`WebClientOrigins`) — o compose já passa `WEB_PUBLIC_URL` do `.env` pra
isso automaticamente.

## OpenSSL 3.0 e certificados A1 legados

Os `Dockerfile` de `api` e `worker` já vêm com o "legacy provider" do
OpenSSL habilitado (mesmo ajuste manual documentado em
`DEPLOY_LINUX.md` seção 9) — necessário pra carregar certificados A1
ICP-Brasil com algoritmos legados (RC2-40-CBC/3DES), que o OpenSSL 3.0
(padrão nas imagens Debian usadas aqui) rejeita por padrão.

## Sem TLS

Os containers servem tudo em HTTP puro — não há reverse proxy/TLS
configurado no `docker-compose.yml`. Se for expor isso fora de uma rede
interna, coloque um reverse proxy (Nginx, Caddy, Traefik) na frente
terminando HTTPS, apontando pros mesmos containers `web`/`api`.

## Instalar em outra máquina via GHCR (sem clonar o código lá)

O workflow `.github/workflows/docker-publish.yml` builda e publica as 3
imagens da aplicação (`api`, `worker`, `web`) no GitHub Container Registry a
cada push na `main` (ou disparo manual via aba Actions), como
`ghcr.io/aggille/aggilledfe-api:latest` (e `:worker`/`:web`, com tag extra
pelo SHA do commit).

No servidor de destino (Linux, já com Docker instalado), não precisa clonar
o repositório inteiro — só copiar `docker-compose.prod.yml` e `.env`
(preenchido a partir do `.env.example`, mesmas variáveis do compose normal):

```bash
docker login ghcr.io -u <usuario-github> -p <personal-access-token>   # escopo read:packages
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

O PAT (`personal access token`) precisa do escopo `read:packages`; se o
repositório for privado, o pacote publicado também nasce privado — gere o
token numa conta com acesso ao repositório/organização. Pra atualizar depois
de uma nova publicação, ver seção "Atualizando os containers" abaixo.

As mesmas observações de volumes de certificados/XMLs, caminhos dentro do
container e ausência de TLS (seções acima) valem também para o
`docker-compose.prod.yml`.

## Atualizando os containers quando o sistema muda

O jeito de atualizar depende de qual dos dois `docker-compose*.yml` você
está usando.

### Ambiente que builda a partir do código-fonte (`docker-compose.yml`)

Depois de puxar (`git pull`) as alterações mais recentes do repositório, é
só rodar de novo o mesmo comando do primeiro passo a passo — o Compose
rebuilda só as imagens cujo `Dockerfile`/contexto mudou (usa cache de layer
normalmente) e recria só os containers correspondentes:

```bash
docker compose up --build -d
```

ou `docker-up.bat` no Windows (mesma chamada). Não precisa `down` antes —
`up -d` já recria o container em cima do que mudou, sem derrubar os outros
serviços do compose.

### Ambiente publicado via GHCR (`docker-compose.prod.yml`)

Aqui não tem código-fonte local pra rebuildar — o fluxo é: alguém builda e
publica as imagens novas no GHCR (push na `main`, o workflow
`.github/workflows/docker-publish.yml` cuida disso sozinho), e no servidor
de destino você só puxa as imagens novas e recria os containers em cima
delas:

```bash
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d --force-recreate
docker image prune -f
```

- `pull` baixa as imagens `:latest` mais recentes do GHCR.
- `--force-recreate` garante que os containers sejam recriados mesmo com a
  tag `:latest` "igual" à anterior (o que muda é o digest da imagem por
  trás da tag, não o nome) — sem essa flag o Compose às vezes já detecta a
  mudança sozinho, mas usar a flag deixa explícito e não depende disso.
- `docker image prune -f` limpa as camadas/imagens antigas que ficaram
  órfãs depois do `pull`, evitando que o disco do servidor encha com
  versões velhas acumuladas a cada atualização.

Se `docker-compose.prod.yml` também mudou (nova variável de ambiente, novo
volume, etc.), copie a versão atualizada do arquivo pro servidor **antes**
do `pull`/`up -d` acima — senão o servidor continua rodando com a
configuração antiga.

Pro servidor do cliente específico já cadastrado (`172.16.0.3`), esses três
comandos (mais a cópia do `docker-compose.prod.yml`) já estão
automatizados no `update-cliente.bat` da raiz do repositório — só rodar
esse `.bat`, que ele pede a senha do `root` via SSH/SCP quando precisar.

### Migrations na atualização

Não precisa rodar `dotnet ef database update` manualmente em nenhum dos
dois casos — o container `api` já aplica as migrations pendentes sozinho
toda vez que sobe (ver seção "Migrations via EF Core bundle" acima), então
um `up -d`/atualização de imagem já cobre isso.

## O que este setup NÃO cobre

- `AggilleDFe.Maui` (app companion) — fora do escopo de containers de
  servidor, publicado à parte.
- Backup do volume `aggilledfe-db-data` — configure separadamente
  (`pg_dump` agendado, snapshot do volume, etc.), não incluso aqui.
