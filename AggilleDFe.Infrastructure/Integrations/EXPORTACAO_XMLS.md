# Exportação de XMLs do período (ZIP)

Serviço (`XmlExportService`, em
`AggilleDFe.Infrastructure/Integrations/XmlExportService.cs`) que monta um
ZIP com os XMLs de NFe e CTe já armazenados (banco/disco) de um período —
exposto em
`GET /api/v1/xmls/exportar-zip?ano=&mes=&empresaId=&usarDataDownload=` (uso
interno, sem autenticação — mesmo padrão dos demais endpoints de
`XmlEndpoints`), tela "Exportar XMLs" do Blazor.

Diferente de:
- **Baixar XMLs** (`AcessoBaixarXml`) — dispara o download via SEFAZ
  (Distribuição DFe), busca documentos novos.
- **XMLS Baixados** (`AcessoXmlsBaixados`) — só lista/visualiza o que já
  foi baixado, um XML de cada vez.

Esta tela só **empacota o que já está salvo** (não fala com a SEFAZ),
permissão própria `AcessoExportarXmls`.

## Como funciona

1. Recebe `ano`, `mes` (1-12), `empresaId` opcional (`null` = todas as
   empresas) e `usarDataDownload` (bool, padrão `false`).
2. Calcula o primeiro e último dia do mês e chama
   `IXmlRepository.PesquisarAsync(...)` usando um dos dois pares de filtro
   de data conforme `usarDataDownload`:
   - `false` (padrão) — passa o intervalo como `emissaoInicial`/
     `emissaoFinal` (`dataInicial`/`dataFinal` ficam `null`): filtro por
     **emissão** do documento, mesma base da organização de pastas em
     disco (ver `CaminhoXmlHelper`).
   - `true` — passa o intervalo como `dataInicial`/`dataFinal`
     (`emissaoInicial`/`emissaoFinal` ficam `null`): filtro pela data em
     que a plataforma **baixou** o XML (`Xml.DataDownload`), útil quando o
     usuário quer "tudo que baixei em julho", independente da competência
     fiscal do documento.
3. Filtra o resultado em memória pra só `Modelo == "55"` (NFe) ou
   `"57"` (CTe) — ignora outros modelos se existirem.
4. Se não achar nenhum, devolve erro (não gera zip vazio).
5. Para cada XML, reaproveita `IXmlArquivoService.ObterXmlBrutoAsync`
   (mesma lógica banco→disco do botão "Baixar XML" da tela XMLS Baixados)
   — se um documento específico não for encontrado nem no banco nem em
   disco, é só pulado (não interrompe o zip inteiro).
6. Monta o zip em memória (`System.IO.Compression.ZipArchive`), uma
   entrada por XML, caminho `{Cnpj}/{NFe|CTe}/{chave}.xml` (CNPJ sempre no
   caminho, mesmo com uma única empresa — fica pronto pra abrir mais de
   uma pasta sem ambiguidade).
7. Nome do arquivo final: `XMLs_{ano}-{mes:D2}.zip`, ou
   `XMLs_{cnpj}_{ano}-{mes:D2}.zip` quando uma empresa específica foi
   escolhida.

## Limitações conhecidas

- Síncrono: a requisição só responde quando o zip inteiro estiver pronto
  (sem geração em background/notificação). Para os volumes típicos desse
  sistema (texto puro, um mês de uma ou algumas empresas) isso é rápido —
  não tem o custo de rede de falar com a SEFAZ, só leitura de banco/disco.
- Não cobre XMLs de outros modelos além de NFe (55) e CTe (57).
