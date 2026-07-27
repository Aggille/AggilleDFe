# Tela de Baixar XMLs

# Tela de disparo manual, em lote, da Distribuição DFe para todas as empresas elegíveis

# Conteúdo da tela:

- Um botão "Executar Agora" que chama `POST /api/v1/empresas/baixar-xmls`
  (ver `AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_LOTE.md`) — a
  mesma rotina que o `AggilleDFe.Worker` roda sozinho em cada ciclo, só que
  disparada manualmente, sem esperar o próximo horário agendado.
- Usa um `HttpClient` próprio com timeout de 30 min (igual ao botão
  individual em `Empresas.razor`, `E MPRESAS.md`) — a chamada pode demorar
  bastante dependendo de quantas empresas/documentos existirem.
- Mostra o resultado (`ResultadoDistribuicaoLoteDto`, ver
  `AggilleDFe.Application/DTOs/RESULTADO_DISTRIBUICAO_LOTE_DTO.md`) num
  `MudAlert` — sucesso (verde) se nenhuma empresa deu erro, aviso (amarelo)
  se alguma deu.
- Acessível pelo menu lateral ("Baixar XMLs") e por um card na Home.
- Diferente do botão "Baixar XMLs" da tela de Empresas (que processa **uma**
  empresa por vez, `POST /api/v1/empresas/{id}/baixar-xmls`), esta tela
  processa **todas** as empresas elegíveis de uma vez, respeitando
  `Configuracao.ProcessarIndividualmente` (sequencial vs. paralelo).
