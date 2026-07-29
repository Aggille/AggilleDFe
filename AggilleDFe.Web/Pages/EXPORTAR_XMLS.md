# Tela de Exportar XMLs

# Tela que gera um .zip com os XMLs de NFe/CTe já baixados de um mês/ano

# Só acessível por quem tem a permissão `AcessoExportarXmls`
# (`[Authorize(Policy = "ExportarXmls")]`, oculta no `NavMenu` pra quem não
# tem essa claim) — separada de "Baixar XMLs" (SEFAZ) e "XMLS Baixados"
# (grid de visualização), ver
# `AggilleDFe.Infrastructure/Integrations/EXPORTACAO_XMLS.md`

# Conteúdo da tela:

- Seletor de Empresa (mesmo padrão da tela XMLS Baixados: "Todas as
  empresas" + lista vinda de `GET /api/v1/empresas`)
- Seletor de Mês (Janeiro–Dezembro) e campo numérico de Ano (padrão: mês/
  ano atuais)
- Seletor "Filtrar pela data de": Emissão do documento (padrão) ou Download
  (data em que a plataforma baixou o XML) — vira o parâmetro
  `usarDataDownload` no endpoint
- Botão "Exportar" (desabilitado enquanto `_exportando` é true, com
  `MudProgressCircular` indeterminado + texto "Compactando os XMLs,
  aguarde..." ao lado — o zip pode levar alguns segundos num período com
  muitos documentos):
  1. Chama `GET /api/v1/xmls/exportar-zip?ano=&mes=&empresaId=&usarDataDownload=` via
     `HttpClient` só pra conferir se deu certo (mostra o erro num Snackbar
     se não achar nada no período/empresa, em vez de abrir uma aba com
     JSON cru)
  2. Se deu certo, navega (`NavigationManager.NavigateTo(...,
     forceLoad: true)`) pra essa mesma URL — navegação de browser puro,
     que dispara o download do .zip nativamente (o endpoint devolve
     `Content-Disposition: attachment`), sem passar pelo timeout do
     `HttpClient`/`TokenAuthorizationHandler`
