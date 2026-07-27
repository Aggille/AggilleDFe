# DTO de Resultado do Download de XMLs (ResultadoBaixarXmlsDto)

Retorno de `POST /api/v1/empresas/{id}/baixar-xmls`, que dispara a execução
manual da Distribuição DFe (NFe e CTe) para a empresa, via Zeus DFe.NET. Ver
`AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md` para os detalhes
do algoritmo.

- XmlsBaixadosNfe: int — quantidade de documentos NFe completos (`nfeProc`) baixados e salvos em disco nesta execução
- XmlsBaixadosCte: int — quantidade de documentos CTe completos (`cteProc`) baixados e salvos em disco nesta execução
- EventosProcessados: int — quantidade de resumos/eventos processados (manifestações, cancelamentos, resumos sem manifestação), sem contar os documentos completos já computados acima
- Mensagem: string? — resumo textual da execução, exibido no Snackbar da tela de Empresas
