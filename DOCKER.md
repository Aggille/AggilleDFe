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
de uma nova publicação: `docker compose -f docker-compose.prod.yml pull &&
docker compose -f docker-compose.prod.yml up -d` de novo.

As mesmas observações de volumes de certificados/XMLs, caminhos dentro do
container e ausência de TLS (seções acima) valem também para o
`docker-compose.prod.yml`.

## O que este setup NÃO cobre

- `AggilleDFe.Maui` (app companion) — fora do escopo de containers de
  servidor, publicado à parte.
- Backup do volume `aggilledfe-db-data` — configure separadamente
  (`pg_dump` agendado, snapshot do volume, etc.), não incluso aqui.
