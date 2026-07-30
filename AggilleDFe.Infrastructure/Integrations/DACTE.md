# DACTE em PDF (DacteService)

Serviço (`DacteService`, em
`AggilleDFe.Infrastructure/Integrations/DacteService.cs`) que monta o HTML do
DACTE de um CTe já baixado, pela chave — exposto em
`GET /api/v1/xmls/{chave}/dacte` (uso interno, sem autenticação, botão "Ver
DACTE" da tela XMLs Baixados, abre em nova aba via
`IJSRuntime.InvokeVoidAsync("open", url, "_blank")`). Assim como o DANFE de
NF-e (`DanfeService`/`DANFE.md`), o endpoint converte esse HTML em **PDF de
verdade** via `PuppeteerHtmlToPdfService` (ver `PDF.md`) antes de devolver.

## Por que layout próprio, sem pacote pronto

Diferente do DANFE de NF-e (que usa `Zeus.Net.NFe.Danfe.Html`), **não existe
nenhum pacote Zeus.Net publicado no NuGet pra DACTE** — confirmado buscando
`Zeus.Net.CTe.Dacte.*` (nenhum resultado), documentado como limitação em
`DANFE.md`. Só existe código-fonte no próprio repositório do DFe.NET
(`CTe.Dacte.Base`/`CTe.Dacte.Fast`/`CTe.Dacte.OpenFast`), nunca publicado como
pacote, e que depende do motor `FastReport.OpenSource` (relatórios `.frx`) —
integração bem mais trabalhosa e com dependências pesadas. Decisão: montar o
HTML à mão em `DacteHtmlBuilder`, lendo os campos direto do
`CTe.Classes.cteProc` já desserializado (mesma classe usada na Distribuição
DFe, ver `DISTRIBUICAO_DFE.md`) — mesma filosofia leve do DANFE em HTML (sem
`System.Drawing.Common`, funciona em Linux/Docker).

## Como funciona

1. Busca `Xml` pela chave; exige `Modelo == "57"` (só CTe) e conteúdo
   disponível — prefere `Xml.ConteudoXml` (banco), cai para o arquivo em
   `NomeXml` no disco se estiver vazio (mesma prioridade banco→disco do
   DANFE e de `XmlArquivoService`, ver `XMLS.md`).
2. Desserializa com `FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>`.
3. `DacteHtmlBuilder.Montar(cteProc, cancelada)` monta a string HTML (CSS
   inline, sem dependências externas) com: cabeçalho (emitente, modelo/série/
   número, emissão, chip "CANCELADA" quando aplicável), chave de acesso
   agrupada em blocos de 4 dígitos (sem código de barras — ver limitação
   abaixo), protocolo de autorização, natureza da operação/tipo de serviço/
   forma de pagamento/tomador do serviço, remetente/destinatário, percurso
   (município início/fim, CFOP), componentes do valor da prestação
   (`vPrest.Comp`) com total, e observações (`compl.xObs`, se houver).
   - **Tomador do serviço** (quem paga o frete): `ide.tomaBase3.toma`
     (enum `CTe.Classes.Informacoes.Tipos.toma` — Remetente/Expedidor/
     Recebedor/Destinatario/Outros). Quando `Outros`, complementa com o
     nome de `ide.toma4.xNome` (dados do terceiro pagador) entre
     parênteses.

## Limitações conhecidas

- **Sem código de barras**: gerar o Code128 exigiria trazer uma lib de
  barcode (`NetBarcode`, já usada transitivamente pelo DANFE) como
  dependência direta — fora do escopo pedido ("DACTE personalizado"). A
  chave aparece por extenso, agrupada em blocos de 4 dígitos (mesmo formato
  que acompanha o código de barras no documento oficial).
- **Campos específicos de modal não cobertos**: o layout cobre os campos
  comuns a qualquer CTe (o essencial pra conferência/impressão) — detalhes
  específicos de modal aéreo/aquaviário/ferroviário/dutoviário/rodoviário
  (`infCTeNorm.infModal`) não são exibidos.
- **Sem logo da empresa** (mesma limitação do DANFE — campo não existe na
  entidade `Empresa` hoje).
- Só documento completo (`Modelo == "57"` com conteúdo baixado) — resumo
  (`Situacao == "Resumo (schema: ...)"`, ver `DISTRIBUICAO_DFE.md`) não tem
  DACTE.
