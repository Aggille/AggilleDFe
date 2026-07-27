# DANFE em HTML (DanfeService)

Serviço (`DanfeService`, em
`AggilleDFe.Infrastructure/Integrations/DanfeService.cs`) que gera o DANFE de
uma NFe já baixada, pela chave, em **HTML pronto para impressão** — exposto
em `GET /api/v1/xmls/{chave}/danfe` (uso interno, sem autenticação, botão
"Ver DANFE" da tela XMLs Baixados, abre em nova aba via
`IJSRuntime.InvokeVoidAsync("open", url, "_blank")`; o usuário imprime ou
salva como PDF pelo próprio navegador, Ctrl+P).

## Por que HTML, e não PDF direto

O pacote Zeus.Net já usado no projeto para NFC-e/relatórios
(`Zeus.Net.NFe.Danfe.QuestPdf`) **não** gera o DANFE completo (A4) de NF-e —
confirmado por reflexão nos tipos públicos do pacote (só tem
`DanfeNfceDocument`, `EventoNfeDocument` e os relatórios "para contadores").
As alternativas do próprio Zeus.Net com DANFE completo de NF-e em PDF
(`Zeus.Net.NFe.Danfe.PdfClown`) dependem de `System.Drawing.Common`, que só
funciona em Windows — quebraria a exigência do projeto de rodar em
Linux/Docker (`CLAUDE.md`). `Zeus.Net.NFe.Danfe.Html` (usado aqui) não tem
essa dependência — só `NetBarcode` (código de barras 100% gerenciado) — e
funciona em qualquer SO. Decisão confirmada com o usuário.

## Vulnerabilidade de dependência transitiva corrigida

`NetBarcode` traz `SixLabors.ImageSharp` 2.1.1 transitivamente, que tem CVEs
de severidade alta/moderada já corrigidas na série 2.1.x. Foi adicionada uma
referência direta a `SixLabors.ImageSharp` 2.1.13 em
`AggilleDFe.Infrastructure.csproj` só para forçar essa versão mais nova na
resolução do NuGet (maior versão explícita vence sobre a transitiva) — sem
isso, `dotnet build`/`dotnet restore` avisam `NU1902`/`NU1903`.

## Como funciona

1. Busca `Xml` pela chave; exige `Modelo == "55"` (só NFe) e `NomeXml`
   preenchido com um arquivo existente em disco (documento completo já
   baixado — resumo não tem DANFE).
2. Lê o XML do disco e desserializa com
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

## Limitações conhecidas

- Só NFe — CTe usa outro documento (DACTE), **não implementado**: não existe
  nenhum pacote Zeus.Net publicado no NuGet pra DACTE (confirmado buscando
  `Zeus.Net.CTe.Dacte.*` — nenhum resultado). Só existe código-fonte no
  próprio repositório do DFe.NET (`CTe.Dacte.Base`/`CTe.Dacte.Fast`/
  `CTe.Dacte.OpenFast`), nunca publicado como pacote, e que depende do motor
  `FastReport.OpenSource` (relatórios `.frx`) — integração bem mais
  trabalhosa que o pacote HTML pronto usado aqui pro DANFE de NF-e.
  Decisão confirmada com o usuário: deixar de fora por enquanto; o botão
  "Ver DANFE" da tela XMLs Baixados continua só para `Modelo == "55"`.
- Sem logo da empresa (campo não existe na entidade `Empresa` hoje).
