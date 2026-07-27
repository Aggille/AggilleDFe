# Tela de XMLS Baixados

# Tela de consulta dos XMLs (NFe/CTe) baixados, com filtros por empresa e por período

# Classe D.O. XML

# Conteúdo da tela:

- Filtros no topo: dropdown de Empresa (`MudSelect`, populado via `GET
  /api/v1/empresas`, com opção "Todas as empresas"), dropdown de Tipo de
  Documento (`MudSelect`: Todos/NFe/CTe → `null`/`"55"`/`"57"`), dois
  `MudDatePicker` (Baixado de / Baixado até, padrão: últimos 7 dias), um
  `MudTextField` de Fornecedor (busca parcial, case-insensitive, Enter
  dispara a pesquisa) e um botão Pesquisar.
- `GET /api/v1/xmls?empresaId=&dataInicial=&dataFinal=&modelo=&fornecedor=`
  (ver `AggilleDFe.Application/DTOs/XML_DTO.md`) — filtros opcionais e
  combináveis; o período filtra por `DataDownload` (data em que o XML foi
  efetivamente baixado), não pela data de emissão do documento; fornecedor
  filtra por `ILIKE '%valor%'` em `FornecedorNome`.
- Datagrid com as colunas: Empresa (nome resolvido client-side a partir da
  lista de empresas já carregada para o filtro), Modelo (NFe/CTe/NFCe,
  mapeado de `"55"`/`"57"`/`"65"`), Número/Série, Chave, Fornecedor/Emitente,
  Emissão, Baixado em, Valor Total, Situação (chip vermelho "Cancelada"
  quando `Cancelada == true`, senão chip verde com o valor de `Situacao` ou
  "Documento completo"), e uma coluna de Ações.
- Ações por linha (ver `AggilleDFe.Infrastructure/Integrations/MANIFESTACAO.md`):
  - **Baixar XML** (todas as linhas): `NavigationManager.NavigateTo(...,
    forceLoad: true)` para `GET /api/v1/xmls/{chave}/arquivo` — o endpoint
    devolve `Content-Disposition: attachment`, então isso dispara o download
    do navegador direto, sem JS interop.
  - **Ver DANFE** (só `Modelo == "55"`): `IJSRuntime.InvokeVoidAsync("open",
    url, "_blank")` para `GET /api/v1/xmls/{chave}/danfe` — abre o DANFE em
    HTML numa aba nova (ver `AggilleDFe.Infrastructure/Integrations/DANFE.md`);
    o usuário imprime/salva como PDF pelo próprio navegador.
  - **Ciência da Operação** (só `Modelo == "55"`): confirmação simples
    (`ShowMessageBoxAsync`) → `POST /api/v1/xmls/{chave}/manifestacao/ciencia`.
  - **Desconhecimento da Operação** / **Operação Não Realizada** (só
    `Modelo == "55"`): abrem `MotivoManifestacaoDialog`
    (`AggilleDFe.Web/Dialogs/MotivoManifestacaoDialog.razor`, valida 15–255
    caracteres antes de habilitar Confirmar) → `POST
    /api/v1/xmls/{chave}/manifestacao/desconhecimento` ou `.../nao-realizada`
    com o motivo digitado.
  - Essas 3 ações de manifestação usam os endpoints **sem autenticação** do
    grupo `/api/v1/xmls` (mesmo modelo dos demais endpoints internos,
    confiança via CORS) — não os equivalentes protegidos por Basic Auth em
    `/api/v1/dfe`, que são só para integração externa.
  - Toda ação recarrega a grid (`Pesquisar()`) ao final, para refletir a
    situação atualizada.
- Cada linha corresponde a um registro em `XMLS` criado/atualizado pelo
  `DistribuicaoDfeService` durante a rotina de "Baixar XMLs" (ver
  `AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`) — inclui
  tanto documentos completos baixados quanto resumos ainda pendentes de
  manifestação/documento completo (`Situacao == "Resumo"`).
