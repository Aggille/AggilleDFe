# DANFE em PDF (DanfeService)

Serviço (`DanfeService`, em
`AggilleDFe.Infrastructure/Integrations/DanfeService.cs`) que monta o HTML do
DANFE de uma NFe já baixada, pela chave — exposto em
`GET /api/v1/xmls/{chave}/danfe` (uso interno, sem autenticação, botão "Ver
DANFE" da tela XMLs Baixados, abre em nova aba via
`IJSRuntime.InvokeVoidAsync("open", url, "_blank")`). O endpoint converte
esse HTML em **PDF de verdade** antes de devolver, via
`PuppeteerHtmlToPdfService` (ver `PDF.md`) — não é mais "HTML pra imprimir
com Ctrl+P".

## Por que HTML por dentro, e PDF via Chromium (não os pacotes de PDF do Zeus.Net)

O pacote Zeus.Net já usado no projeto para NFC-e/relatórios
(`Zeus.Net.NFe.Danfe.QuestPdf`) **não** gera o DANFE completo (A4) de NF-e —
confirmado por reflexão nos tipos públicos do pacote (só tem
`DanfeNfceDocument`, `EventoNfeDocument` e os relatórios "para contadores").
As alternativas do próprio Zeus.Net com DANFE completo de NF-e em PDF
(`Zeus.Net.NFe.Danfe.PdfClown`) dependem de `System.Drawing.Common`, que só
funciona em Windows — quebraria a exigência do projeto de rodar em
Linux/Docker (`CLAUDE.md`). `Zeus.Net.NFe.Danfe.Html` (usado aqui) não tem
essa dependência — só `NetBarcode` (código de barras 100% gerenciado) — e
funciona em qualquer SO.

Por isso o HTML gerado pelo Zeus.Net continua sendo a fonte da formatação
(layout do DANFE), mas a resposta final do endpoint é PDF: o
`PuppeteerHtmlToPdfService` roda um Chromium headless (100% Linux-compatível,
sem `System.Drawing.Common`) que renderiza esse HTML e exporta PDF, evitando
a impressão via navegador do usuário (que ficava ruim/inconsistente entre
navegadores).

## Vulnerabilidade de dependência transitiva corrigida

`NetBarcode` traz `SixLabors.ImageSharp` 2.1.1 transitivamente, que tem CVEs
de severidade alta/moderada já corrigidas na série 2.1.x. Foi adicionada uma
referência direta a `SixLabors.ImageSharp` 2.1.13 em
`AggilleDFe.Infrastructure.csproj` só para forçar essa versão mais nova na
resolução do NuGet (maior versão explícita vence sobre a transitiva) — sem
isso, `dotnet build`/`dotnet restore` avisam `NU1902`/`NU1903`.

## Como funciona

1. Busca `Xml` pela chave; exige `Modelo == "55"` (só NFe) e conteúdo
   disponível (documento completo já baixado — resumo não tem DANFE):
   prefere `Xml.ConteudoXml` (banco); se vazio, cai para o arquivo em
   `NomeXml` no disco (registros antigos, gravados antes desse campo
   existir) — mesma prioridade banco→disco de `XmlArquivoService`, ver
   `XMLS.md`.
2. Desserializa o conteúdo com
   `FuncoesXml.XmlStringParaClasse<NFe.Classes.nfeProc>` (mesmo padrão usado
   em `DISTRIBUICAO_DFE.md`).
3. Monta `NFe.Danfe.Html.Dominio.DanfeNFe(nfeProc.NFe, status, protocolo,
   creditos, issqn, logo)` — o construtor da lib já extrai
   emitente/destinatário/produtos/impostos direto do objeto `NFe.Classes.NFe`
   (não precisou mapear campo a campo manualmente). `status` é
   `Status.Cancelada` quando `Xml.Cancelada == "S"`, senão
   `Status.Autorizada`. `issqn`/`logo` ficam vazios (a `Empresa` não guarda
   logo hoje).
4. `new DanfeNfeHtml2(danfeNFe).ObterDocHtmlAsync()` retorna um `Documento`
   cujo `.Html` é a string HTML completa, devolvida como `text/html`.

## Bug de valores 100x maiores (corrigido)

O Zeus.Net (`NFe.Danfe.Html/CrossCutting/Utils.cs`, método
`FormatarNumeroDanfe`, usado por `DanfeNfeHtml2` pra preencher os valores no
template HTML) monta a string do valor com vírgula decimal e chama
`double.TryParse(str, out var result)` **sem especificar `CultureInfo`** —
depende da cultura ambiente da thread ter vírgula como separador decimal
pra dar certo. Se a cultura ambiente não for uma dessas (ex.: Invariant, ou
qualquer outra com "." decimal), a vírgula é lida como separador de milhar
e o valor sai **100x maior** (ex.: R$ 57.392,17 vira R$ 5.739.217,00) — o
XML e o `Xml.ValorTotal` gravado no banco continuam corretos, só o HTML do
DANFE mostra errado, já que ele reparsa o XML na hora (`ObterDanfeHtmlAsync`
não usa o valor já salvo no banco).

Corrigido em `DanfeService.ObterDanfeHtmlAsync` forçando
`CultureInfo.CurrentCulture`/`CurrentUICulture` pra `"pt-BR"` só ao redor da
desserialização/renderização do DANFE (flui corretamente através dos
`await` internos do Zeus via `ExecutionContext` — setar só
`CultureInfo.DefaultThreadCurrentCulture`, que afeta apenas thread **novas**,
não teve efeito nos testes). Não precisou mexer em
`DistribuicaoDfeService`/`XmlImportService` — a desserialização do
`FuncoesXml.XmlStringParaClasse` em si (que grava `Xml.ValorTotal`) usa
parsing seguro (`XmlConvert`/`XmlSerializer`) e não depende de cultura;
nem em `DacteService`/`DacteHtmlBuilder` — o DACTE não usa o
`FormatarNumeroDanfe` do Zeus (é HTML próprio, formata os valores com
`CultureInfo.GetCultureInfo("pt-br")` explícito desde sempre).

## Limitações conhecidas

- Só NFe — CTe usa outro documento (DACTE), implementado à parte em
  `DacteService`/`DACTE.md` (layout próprio, sem pacote Zeus.Net — não existe
  nenhum publicado pra DACTE). O botão "Ver DACTE" da tela XMLs Baixados é
  exibido só para `Modelo == "57"`, ao lado (não em substituição) de "Ver
  DANFE", que continua só para `Modelo == "55"`.
- Sem logo da empresa (campo não existe na entidade `Empresa` hoje).
