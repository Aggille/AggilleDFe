# Manual de Deploy e Configuração — AggilleDFe (Windows)

Guia para colocar a plataforma AggilleDFe em produção em um servidor
**Windows**. Cobre os três componentes que rodam continuamente:

- **AggilleDFe.API** — API ASP.NET Core (minimal APIs), expõe os endpoints
  REST e o Swagger (`/swagger`)
- **AggilleDFe.Web** — front-end Blazor WebAssembly **standalone** (sem
  servidor próprio — publica como arquivos estáticos)
- **AggilleDFe.Worker** — serviço de background para o ciclo agendado de
  download de XMLs

`AggilleDFe.Maui` é o app companion e não faz parte deste guia de deploy de
servidor. Para deploy em Linux, ver `DEPLOY_LINUX.md`.

---

## 1. Pré-requisitos

| Item | Versão / detalhe |
|---|---|
| .NET SDK/Runtime | .NET 10 (`dotnet --version` deve reportar 10.x). Em produção, o **ASP.NET Core Runtime** 10 basta para API/Worker; para publicar o Web também é necessário o SDK completo (ou publicar em outra máquina e só copiar o resultado). |
| Banco de dados | PostgreSQL (qualquer versão recente com suporte a `time without time zone`). Nome de banco convencionado: `aggilledfe`. Pode rodar no mesmo servidor Windows ou em outro. |
| Certificados digitais A1 (.pfx) | Um por empresa cadastrada, acessíveis pelo caminho de arquivo configurado no cadastro da empresa. |
| Schemas XSD do Zeus DFe.NET | Pasta plana de `.xsd` (ver seção 6) — opcional, mas recomendada. |
| IIS | Necessário para hospedar o `AggilleDFe.Web` (arquivos estáticos do Blazor WASM). |

---

## 2. Build e publish

Os três componentes (API, Web, Worker) são publicados separadamente — cada
`dotnet publish` monta a árvore de arquivos de UM projeto — mas com `-o`
apontando para subpastas de uma única pasta de saída, tudo fica organizado
em um só lugar (`./publish`), pronto para ser copiado/zipado inteiro para o
servidor de destino.

A partir da raiz do repositório (PowerShell):

```powershell
Remove-Item -Recurse -Force .\publish -ErrorAction SilentlyContinue
dotnet publish AggilleDFe.API\AggilleDFe.API.csproj       -c Release -o .\publish\api
dotnet publish AggilleDFe.Web\AggilleDFe.Web.csproj       -c Release -o .\publish\web
dotnet publish AggilleDFe.Worker\AggilleDFe.Worker.csproj -c Release -o .\publish\worker
```

Resultado:

```
publish/
├── api/          # AggilleDFe.API.dll + dependências (roda com "dotnet AggilleDFe.API.dll")
├── web/
│   └── wwwroot/  # arquivos estáticos do Blazor WASM — isso aqui é o que vai pro IIS
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

Depois de publicado, compacte a pasta inteira para transferir ao servidor:

```powershell
Compress-Archive -Path .\publish\* -DestinationPath aggilledfe-publish.zip
```

Os binários publicados são "framework-dependent" (o padrão) — exigem o
ASP.NET Core Runtime 10 instalado na máquina de destino.

---

## 3. Banco de dados

1. Criar o banco `aggilledfe` no PostgreSQL (usuário/senha à sua escolha).
2. Aplicar as migrations. Duas opções:
   - **Via `dotnet ef`** (precisa do SDK e do código-fonte na máquina que
     roda o comando — pode ser a máquina de deploy ou uma máquina de build,
     apontando para o banco de produção via connection string):
     ```powershell
     dotnet tool install --global dotnet-ef   # se ainda não tiver
     dotnet ef database update --project AggilleDFe.Infrastructure --startup-project AggilleDFe.API
     ```
   - **Via script SQL gerado offline** (não precisa do SDK na máquina do
     banco):
     ```powershell
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

Exemplo de variável de ambiente (PowerShell):
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=meuservidor;Port=5432;Database=aggilledfe;Username=aggilledfe;Password=SENHA_REAL"
```

Serviços do Windows **não herdam** variáveis de sessão de usuário — para um
serviço registrado com `sc.exe` (seção 7), configure a variável como
**variável de ambiente da máquina** (Painel de Controle → Sistema →
Configurações Avançadas → Variáveis de Ambiente) ou use
`appsettings.Production.json` ao lado do executável, que funciona
independente de variáveis de ambiente.

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
o processo da API (a conta do serviço do Windows, seção 7) tenha permissão
de **leitura** nesses caminhos.

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
diretamente: coloque a API atrás do **IIS** (com o ASP.NET Core Module,
atuando como reverse proxy) que termina o HTTPS e repassa HTTP puro para o
Kestrel internamente. Configure `ASPNETCORE_URLS` para escutar só em
`http://127.0.0.1:<porta interna>` (ex.: `5007`), e deixe o IIS expor a
porta 443 pública com certificado real.

Se preferir terminar TLS direto no Kestrel (sem IIS na frente), configure em
`appsettings.Production.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:5007",
        "Certificate": { "Path": "C:\\caminho\\para\\cert.pfx", "Password": "..." }
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

## 7. Rodando como Serviço do Windows

A API e o Worker já vêm preparados para rodar como Windows Service — os dois
referenciam o pacote `Microsoft.Extensions.Hosting.WindowsServices` e chamam
`UseWindowsService()`/`AddWindowsService()` no `Program.cs`. **Isso não é
automático** (não vem "de graça" só por rodar em cima do ASP.NET Core) —
sem essa chamada explícita no código, o processo até sobe quando o
`sc.exe start` roda, mas nunca avisa o Gerenciador de Controle de Serviços
que iniciou com sucesso, e o Windows derruba com **erro 1053** ("o serviço
não respondeu à solicitação de início a tempo"). Se você publicou uma
versão do código anterior a essa mudança e bateu nesse erro, republique.

Registre com `sc.exe`:

```powershell
sc.exe create AggilleDFeApi binPath= "C:\AggilleDFe\api\AggilleDFe.API.exe" start= auto
sc.exe start AggilleDFeApi
```

Mesma ideia para o `AggilleDFe.Worker.exe` — registre como
`AggilleDFeWorker`:

```powershell
sc.exe create AggilleDFeWorker binPath= "C:\AggilleDFe\worker\AggilleDFe.Worker.exe" start= auto
sc.exe start AggilleDFeWorker
```

Configure `ConnectionStrings__DefaultConnection` etc. como variáveis de
ambiente **da máquina** (não de sessão de usuário) ou via
`appsettings.Production.json` ao lado do executável (ver seção 3).

Recomendado também configurar reinício automático em caso de falha (não é
feito por padrão):
```powershell
sc.exe failure AggilleDFeApi reset= 86400 actions= restart/60000/restart/60000/restart/60000
sc.exe failure AggilleDFeWorker reset= 86400 actions= restart/60000/restart/60000/restart/60000
```

Erros comuns ao registrar/iniciar o serviço:
- **1073** (`ERROR_SERVICE_EXISTS`) — o serviço já existe; use
  `sc.exe config <nome> binPath= "..."` pra atualizar o caminho, ou
  `sc.exe delete <nome>` antes de criar de novo.
- **1053** — ver o aviso acima sobre `UseWindowsService()`; se o código já
  tem isso e o erro persiste, rode o `.exe` direto num console
  (`.\AggilleDFe.API.exe`) pra ver o erro de inicialização real (connection
  string ausente, banco inacessível, porta já em uso, etc.).

O `AggilleDFe.Web` **não** roda como serviço — é estático. Publique o
conteúdo de `publish/web/wwwroot` como um site no **IIS**:
1. Adicionar o site apontando para a pasta `wwwroot` publicada.
2. Garantir MIME type `application/wasm` para arquivos `.wasm` (o publish já
   gera um `web.config` com isso configurado automaticamente para IIS).
3. Editar `wwwroot/appsettings.json` (no conteúdo publicado) trocando
   `ApiUrl` para a URL pública real da API.

---

## 8. Docker

Implementado — ver **[DOCKER.md](DOCKER.md)** na raiz do repositório. Em
Windows, roda via Docker Desktop com containers Linux (as imagens do
.NET/Nginx usadas são baseadas em Linux) — use `docker-up.bat` (raiz do
repositório) para buildar e subir os 4 serviços (`db`, `api`, `worker`,
`web`) de uma vez.

---

## 9. Checklist de deploy

- [ ] Banco `aggilledfe` criado e migrations aplicadas
- [ ] `ConnectionStrings:DefaultConnection` configurada (fora do git)
- [ ] `WebClientOrigins` apontando para a URL real do Web publicado
- [ ] Pasta `SchemasPath` com os `.xsd` copiados (opcional, mas recomendado)
- [ ] Caminhos de certificado (`.pfx`) e pasta de XMLs de cada empresa
      acessíveis (leitura/escrita) pela conta do Serviço do Windows
- [ ] `wwwroot/appsettings.json` do Web publicado com `ApiUrl` correto
- [ ] TLS configurado (IIS reverse proxy ou Kestrel direto)
- [ ] Serviços `AggilleDFeApi`/`AggilleDFeWorker` registrados com `sc.exe` e
      iniciando automaticamente (`start= auto`)
- [ ] Site do Web servindo os arquivos estáticos publicados no IIS
