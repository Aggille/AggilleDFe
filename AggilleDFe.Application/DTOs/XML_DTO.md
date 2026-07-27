# DTO de XML (XmlDto)

Retorno de `GET /api/v1/xmls`, usado pela tela "XMLS Baixados"
(`AggilleDFe.Web/Pages/XmlsBaixados.razor`) para listar os documentos fiscais
(NFe/CTe) baixados/registrados pelo `DistribuicaoDfeService` (ver
`AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`).

Espelha a entidade `Xml` (ver `AggilleDFe.Domain/Entities/XMLS.md`), exceto
`Cancelada` (`"S"/"N"`/`null` no DO → `bool` no DTO, mesma convenção de
`EmpresaDto.Manifesta`/`Inativo`) e alguns campos de manifestação
(`Descricao`/`Mensagem`/`Situacao`/datas de ciência/realização/desconhecimento)
que não são exibidos nesta tela por não serem relevantes para a listagem de
XMLs baixados — permanecem só na entidade:

- Id, Chave, Protocolo, Emissao, DataDownload, FornecedorNome, FornecedorCnpj,
  FornecedorCidade, FornecedorUf, ValorTotal, ValorIcms, StatusNfe,
  MensagemNfe, NomeXml (caminho do arquivo salvo em disco), Numero, Serie,
  Modelo (`"55"` NFe / `"57"` CTe), EmpresaId, Schema, Situacao,
  DataCancelamento, MotivoCancelamento
- Cancelada: bool — `true` quando a entidade tem `Cancelada == "S"`

## Filtros da consulta (`GET /api/v1/xmls`)

- `empresaId` (int, opcional): filtra por uma empresa específica
- `dataInicial`/`dataFinal` (date, opcionais): filtram pelo campo
  **`DataDownload`** (data em que o XML foi efetivamente baixado — não a data
  de emissão do documento), intervalo inclusivo dos dois lados.
- `emissaoInicial`/`emissaoFinal` (date, opcionais): filtram pelo campo
  **`Emissao`** (data de emissão do documento fiscal), intervalo inclusivo dos
  dois lados — independente de `dataInicial`/`dataFinal`, os dois filtros de
  período podem ser combinados.
- Sem filtros, retorna todas as linhas — a tela aplica um período padrão
  (últimos 7 dias, mesmo critério da tela "Registros") para não carregar a
  tabela inteira de uma vez.
- Ordenação: `DataDownload` decrescente, depois `Id` decrescente (mais
  recentes primeiro).
