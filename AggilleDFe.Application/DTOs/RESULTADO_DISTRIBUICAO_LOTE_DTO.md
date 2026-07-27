# DTO de Resultado da Distribuição em Lote (ResultadoDistribuicaoLoteDto)

Retorno de `POST /api/v1/empresas/baixar-xmls`, que dispara manualmente a
Distribuição DFe (ver `AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`)
para **todas** as empresas elegíveis de uma vez — o mesmo processamento que o
`AggilleDFe.Worker` faz sozinho em cada ciclo (ver
`AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_LOTE.md`), só que
disparado manualmente pelos botões "Baixar XMLs" do Home e do menu lateral.

- EmpresasProcessadas: int — quantas empresas entraram na execução (ativas)
- EmpresasComErro: int — quantas dessas retornaram erro (detalhe de cada uma
  fica em `LOGS`, ver tela Registros)
- XmlsBaixadosNfe: int — soma de XMLs de NFe baixados em todas as empresas
- XmlsBaixadosCte: int — soma de XMLs de CTe baixados em todas as empresas
- Mensagem: string? — resumo textual, exibido no Snackbar/tela
