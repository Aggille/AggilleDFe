# DTO de Log (LogDto)

Retorno de `GET /api/v1/logs`, usado pela tela "Registros" (`AggilleDFe.Web/Pages/Registros.razor`)
para listar as linhas gravadas pelo `DistribuicaoDfeService` (ver
`AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`) — uma linha por
evento relevante (resumo recebido, manifestação realizada, XML baixado,
cancelamento registrado, erro) mais as linhas de resumo por laço (NFe/CTe) de
cada execução.

Espelha 1:1 os campos da entidade `Log` (ver `AggilleDFe.Domain/Entities/LOG.md`):

- Id: int
- Data: DateOnly? — data do evento
- HoraInicio: TimeOnly? — hora do evento (ou início do laço, nas linhas de resumo)
- HoraFinal: TimeOnly? — igual a HoraInicio nas linhas de evento; hora de término do laço nas linhas de resumo
- EmpresaId: int? — id da empresa (a tela resolve o nome via `GET /api/v1/empresas`, não há endpoint dedicado de junção)
- QuantidadeXmls: int? — só preenchido nas linhas de resumo por laço (NFe ou CTe) de cada execução
- Mensagem: string? — descrição textual do evento ou erro
- XmlId: int? — id do registro em `XMLS` relacionado, quando aplicável
- Chave: string? — chave de 44 dígitos da NFe/CTe relacionada, quando aplicável
- Nsu: int? — NSU da consulta de distribuição DFe que originou o evento (só preenchido nas linhas geradas pelo laço de NFe/CTe do `DistribuicaoDfeService`; nulo nas linhas de `ManifestacaoService`, que não tem NSU em escopo)

## Filtros da consulta (`GET /api/v1/logs`)

- `empresaId` (int, opcional): filtra por uma empresa específica
- `dataInicial`/`dataFinal` (date, opcionais): filtram pelo campo `Data`, intervalo inclusivo dos dois lados
- Sem filtros, retorna todas as linhas — a tela aplica um período padrão (últimos 7 dias) para não carregar a tabela inteira de uma vez
- Ordenação: `Data` decrescente, depois `Id` decrescente (mais recentes primeiro)
