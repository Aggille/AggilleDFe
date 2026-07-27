# Worker automático (AggilleDFe.Worker)

`Worker` (`AggilleDFe.Worker/Worker.cs`) é um `BackgroundService` que roda em
ciclo, chamando `IDistribuicaoLoteService.ExecutarTodasAsync(execucaoManual:
false)` (ver `AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_LOTE.md`) —
o mesmo serviço usado pelos botões "Baixar XMLs" do Home/menu lateral
(`POST /api/v1/empresas/baixar-xmls`), só que com `execucaoManual: false`
(respeita a janela `HoraInicial`/`HoraFinal` de cada empresa) em vez de
`true`. Toda a lógica de "quais empresas processar, sequencial ou paralelo"
vive em `DistribuicaoLoteService` — o `Worker` cuida só do agendamento
(esperar `Configuracao.TempoExecucao` e repetir).

## Por que `IServiceScopeFactory` em vez de injetar os repositórios direto

`Worker` é registrado como `BackgroundService` (efetivamente singleton, vive
o tempo todo do processo). `AppDbContext` e todos os repositórios/serviços
são `Scoped` — não podem ser injetados direto no construtor de um singleton.
Por isso o `Worker` recebe só `IServiceScopeFactory` e cria um
`IServiceScope` novo a cada ciclo, de onde resolve `IConfiguracaoRepository`
e `IDistribuicaoLoteService`. O `DistribuicaoLoteService`, por sua vez, cria
seus próprios scopes por empresa (ver `DISTRIBUICAO_LOTE.md`) — obrigatório
no modo paralelo, já que `DbContext` não é thread-safe.

## Ciclo (`ExecuteAsync`/`ExecutarCicloAsync`)

1. Lê `Configuracao` (`IConfiguracaoRepository.ObterAsync()`) — **a cada
   ciclo**, não só na inicialização, para que alterar o "Tempo de Execução"
   pela tela de Configuração valha a partir do próximo ciclo, sem reiniciar
   o serviço (Windows Service / systemd unit).
   - Se não houver `Configuracao` salva ou `TempoExecucao` for nulo/≤0, loga
     aviso e tenta de novo em 5 minutos (intervalo de fallback fixo,
     `TempoExecucaoPadraoMinutos`).
2. Chama `IDistribuicaoLoteService.ExecutarTodasAsync(execucaoManual: false)` —
   filtra empresas ativas dentro da janela, processa sequencial ou paralelo
   conforme `Configuracao.ProcessarIndividualmente`, e agrega o resultado
   (ver `DISTRIBUICAO_LOTE.md` para o algoritmo completo).
3. Loga o resumo (`ILogger`, não `LOGS` — os detalhes por empresa já ficam em
   `LOGS` via `DistribuicaoDfeService`).
4. Aguarda `Configuracao.TempoExecucao` minutos (`Task.Delay`) e repete. Um
   `try/catch` defensivo em volta do ciclo inteiro garante que uma falha
   inesperada não derruba o loop do `Worker`.

## Limitações conhecidas

- Não há mecanismo de "empresa travada" (se uma execução ainda estiver
  rodando quando o próximo ciclo teórico começaria, o `Worker` simplesmente
  não inicia um novo ciclo até o `Task.Delay` do ciclo atual terminar — não
  há execução simultânea de dois ciclos).
- `TempoRetorno`/`IntervaloTentativas`/`QuantidadeTentativas` da `Empresa`
  (campos reservados, ver `ZEUS_CONFIGURACAO.md`) ainda não são usados por
  nenhuma lógica de retentativa — cada ciclo tenta uma vez só.
