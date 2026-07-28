# Distribuição DFe em lote (DistribuicaoLoteService)

Serviço (`DistribuicaoLoteService`, em
`AggilleDFe.Infrastructure/Integrations/DistribuicaoLoteService.cs`) que
processa **todas** as empresas elegíveis de uma vez, chamando
`IDistribuicaoDfeService.ExecutarAsync` (ver `DISTRIBUICAO_DFE.md`) para cada
uma. É a lógica compartilhada entre:

- **`AggilleDFe.Worker`** (`Worker.cs`) — chama a cada ciclo, com
  `execucaoManual: false` (respeita a janela `HoraInicial`/`HoraFinal` de
  cada empresa via `JanelaExecucaoService`).
- **`POST /api/v1/empresas/baixar-xmls`** — botão "Baixar XMLs" do Home e do
  menu lateral (`AggilleDFe.Web/Pages/BaixarXmls.razor`), com
  `execucaoManual: true` (ignora a janela, mesmo critério do botão manual
  por empresa em `Empresas.razor`).

Extraído para um serviço próprio (em vez de duplicar a lógica dentro do
`Worker`) justamente para os dois usos acima compartilharem o mesmo
algoritmo sem divergir.

## Algoritmo

1. Lê `Configuracao` (para `ProcessarIndividualmente`) e todas as empresas.
2. Filtra `Empresa.Inativo != "S"` (empresas ativas) e, dentro dessas,
   separa as elegíveis via
   `JanelaExecucaoService.PodeExecutar(empresa, DateTime.Now, execucaoManual)`.
3. Empresas ativas que ficaram fora da janela (`PodeExecutar` retornou
   `false` — só acontece com `execucaoManual: false`, ou seja, no ciclo
   automático do Worker) geram um registro em `LOGS` com mensagem
   `"Empresa não processada"` (sem chave, sem quantidade de XMLs) — dá
   rastreabilidade de que a empresa foi avaliada no ciclo e propositalmente
   pulada, em vez de ficar silenciosa na tela de Registros. Empresas
   inativas (`Inativo == "S"`) não geram log nenhum — são ignoradas por
   completo, não "puladas".
4. Conforme `Configuracao.ProcessarIndividualmente` (rótulo na tela de
   Configuração: "Processar uma empresa de cada vez"):
   - `"S"` → **rodízio**, vale tanto pro ciclo automático do Worker quanto
     pro botão manual "Baixar XMLs" (todas as empresas): processa só
     **uma** empresa elegível por chamada (a próxima depois de
     `Configuracao.UltimaEmpresaProcessadaId`, na ordem de
     `Empresa.Posicao`/Id, com wrap-around) e gera log
     `"Empresa não processada"` pras demais elegíveis daquela chamada. Na
     próxima chamada (próximo ciclo do Worker, ou próximo clique no botão)
     processa a próxima da fila, e assim por diante. Existe justamente pra
     não bater na SEFAZ com todas as empresas de uma vez (causa comum de
     rejeição cStat 656 "Consumo Indevido"). O único fluxo que foge dessa
     regra é o botão de baixar XMLs de **uma empresa específica** (grid de
     `Empresas.razor`, endpoint `POST /{id}/baixar-xmls`) — não passa por
     `DistribuicaoLoteService`, chama `IDistribuicaoDfeService` direto pra
     aquela empresa, então processa na hora independente do rodízio.
   - Qualquer outro valor de `ProcessarIndividualmente` → processa todas em
     paralelo (`Task.WhenAll`), manual ou automático.
5. **Cada empresa usa seu próprio `IServiceScope`** (via
   `IServiceScopeFactory`) — obrigatório no modo paralelo, já que
   `AppDbContext` não é thread-safe; no modo sequencial não muda o
   comportamento, só mantém o código único para os dois casos.
6. Agrega os resultados (`ResultadoDistribuicaoLoteDto`): quantas empresas
   processadas, quantas com erro, soma de XMLs de NFe/CTe baixados. Erros
   por empresa não abortam o lote — cada `ExecutarAsync` já trata e loga os
   próprios erros (ver `DISTRIBUICAO_DFE.md`); aqui só se conta quantas
   retornaram erro, o detalhe fica em `LOGS` (tela Registros).

Ver `AggilleDFe.Application/DTOs/RESULTADO_DISTRIBUICAO_LOTE_DTO.md` para o
formato do resultado.
