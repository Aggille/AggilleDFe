# Conversão HTML → PDF (PuppeteerHtmlToPdfService)

`IHtmlToPdfService` (`AggilleDFe.Application/Interfaces/IHtmlToPdfService.cs`),
implementado por `PuppeteerHtmlToPdfService` — converte uma string HTML em
PDF de verdade, usado pelos endpoints de DANFE (`DANFE.md`) e DACTE
(`DACTE.md`) em `XmlEndpoints`. Registrado como **singleton** no
`Program.cs` da API (`AddSingleton<IHtmlToPdfService, PuppeteerHtmlToPdfService>`).

## Como funciona

Usa `PuppeteerSharp` para controlar um Chromium headless: mantém um único
processo Chromium aberto (campo `_browser`, lazy, protegido por
`SemaphoreSlim`) reaproveitado entre requisições — abrir/fechar o Chromium a
cada chamada seria caro demais. Cada conversão abre uma aba (`NewPageAsync`),
carrega o HTML (`SetContentAsync`) e exporta com `PdfDataAsync` (formato A4,
fundo/cores preservados, margem de 8mm).

## Chromium: produção (Docker) vs dev local

- **Docker** (`AggilleDFe.API/Dockerfile`): instala o **Google Chrome
  estável** (repositório oficial da Google) na imagem final e define
  `PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable`. **Não** usa o
  pacote `chromium` do Ubuntu/Debian — desde o Ubuntu 19.04 esse pacote é só
  um stub de transição que aciona o snap (`chromium-browser_...snap1...`),
  e falha em qualquer container (sem snapd rodando, sem acesso à snap
  store) — confirmado tentando essa rota antes de trocar pro Chrome oficial.
  O serviço usa o caminho do Chrome instalado em vez de baixar seu próprio
  Chromium — mais leve e não depende de acesso à internet do container em
  runtime (só a imagem precisa de internet, em build time).
- **Dev local** (rodando fora do Docker, sem `PUPPETEER_EXECUTABLE_PATH`
  definida): o serviço baixa um Chromium gerenciado pelo próprio
  PuppeteerSharp (`BrowserFetcher().DownloadAsync()`) na primeira chamada —
  cache em `%LOCALAPPDATA%\.local-chromium` (Windows) ou `~/.cache/puppeteer`
  (Linux/Mac). Só precisa de internet na primeira vez.

## `--no-sandbox`

O container roda o processo como root (sem `USER` dedicado no Dockerfile), e
o sandbox do Chromium se recusa a rodar como root sem `--no-sandbox`/
`--disable-setuid-sandbox`. Sem essas flags, `Puppeteer.LaunchAsync` falha
com erro de sandbox dentro do container.
