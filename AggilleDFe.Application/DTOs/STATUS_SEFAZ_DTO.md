# DTO de Status do SEFAZ (StatusSefazResultadoDto)

Retorno de `GET /api/v1/empresas/{id}/status-sefaz`, que consulta o serviço de
status (`NfeStatusServico`) do SEFAZ da UF da empresa, via Zeus DFe.NET. Ver
`AggilleDFe.Infrastructure/Integrations/ZEUS_CONFIGURACAO.md` para os detalhes
de como a chamada é montada a partir da entidade `Empresa`.

- CStat: int — código de status retornado pelo SEFAZ (107 = Serviço em Operação, etc.)
- XMotivo: string — descrição textual do status
- Uf: string? — UF consultada (a mesma da empresa)
- Ambiente: string? — `"P"` Produção / `"H"` Homologação (o mesmo configurado na empresa)
- VersaoLayout: string? — versão do layout do serviço retornada pelo SEFAZ
- DhRecbto: DateTimeOffset? — data/hora de recebimento da resposta pelo SEFAZ
- TempoMedioMs: int? — tempo médio de resposta do serviço, em milissegundos (quando informado pelo SEFAZ)
