# Tela de Registros

# Tela de consulta dos logs de execução (entidade LOG), com filtros por empresa e por período

# Classe D.O. LOG

# Conteúdo da tela:

- Filtros no topo: dropdown de Empresa (`MudSelect`, populado via `GET
  /api/v1/empresas`, com opção "Todas as empresas"), dois `MudDatePicker`
  (Período de / Período até, padrão: últimos 7 dias) e um botão Pesquisar.
- `GET /api/v1/logs?empresaId=&dataInicial=&dataFinal=` (ver
  `AggilleDFe.Application/DTOs/LOG_DTO.md`) — todos os filtros são opcionais e
  combináveis; sem nenhum filtro retorna todas as linhas (a tela sempre aplica
  um período padrão para evitar carregar a tabela inteira).
- Datagrid com as colunas: Data, Horário (`HoraInicio` — `HoraFinal`, ou só
  `HoraInicio` quando iguais/`HoraFinal` vazio), Empresa (nome resolvido
  client-side a partir da lista de empresas já carregada para o filtro, não
  há endpoint de junção), Qtd. XMLs, Chave, Mensagem.
- Cada linha corresponde a um evento gravado pelo `DistribuicaoDfeService`
  durante a rotina de "Baixar XMLs" (ver
  `AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`): resumo
  recebido, manifestação realizada, XML baixado, cancelamento registrado,
  erro por item, além de uma linha de resumo por laço (NFe/CTe) com
  `QuantidadeXmls` preenchido.
