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
2. Filtra `Empresa.Inativo != "S"` e
   `JanelaExecucaoService.PodeExecutar(empresa, DateTime.Now, execucaoManual)`.
3. Conforme `Configuracao.ProcessarIndividualmente` (rótulo na tela de
   Configuração: "Processar uma empresa de cada vez"):
   - `"S"` → processa as empresas elegíveis sequencialmente.
   - Qualquer outro valor → processa todas em paralelo (`Task.WhenAll`).
4. **Cada empresa usa seu próprio `IServiceScope`** (via
   `IServiceScopeFactory`) — obrigatório no modo paralelo, já que
   `AppDbContext` não é thread-safe; no modo sequencial não muda o
   comportamento, só mantém o código único para os dois casos.
5. Agrega os resultados (`ResultadoDistribuicaoLoteDto`): quantas empresas
   processadas, quantas com erro, soma de XMLs de NFe/CTe baixados. Erros
   por empresa não abortam o lote — cada `ExecutarAsync` já trata e loga os
   próprios erros (ver `DISTRIBUICAO_DFE.md`); aqui só se conta quantas
   retornaram erro, o detalhe fica em `LOGS` (tela Registros).

Ver `AggilleDFe.Application/DTOs/RESULTADO_DISTRIBUICAO_LOTE_DTO.md` para o
formato do resultado.
