# DTO de Configuração (ConfiguracaoDto)

Contrato usado pela API (`GET`/`PUT /api/v1/configuracao`) e pela tela de Configuração.
Representa o único registro de configuração global do sistema (`CONFIGURACAO`).

- Id: int — 0 quando ainda não existe registro salvo
- CnpjEmpresa: string — 14 caracteres alfanuméricos (sem máscara), obrigatório
- NomeEmpresa: string — obrigatório, máx. 60 caracteres
- TempoExecucao: int? — minutos, obrigatório, maior que zero
- PortaApi: int? — obrigatório, entre 1 e 65535
- UsuarioApi: string? — obrigatório somente se `ApiAtiva = true`, máx. 50 caracteres
- SenhaApi: string? — obrigatório somente se `ApiAtiva = true`, máx. 20 caracteres
- ApiAtiva: bool — convertido para "S"/"N" no DO `Configuracao.ApiAtiva`
- ProcessarIndividualmente: bool — convertido para "S"/"N" no DO `Configuracao.ProcessarIndividualmente`

Campos do DO que NÃO fazem parte deste DTO (não editáveis pela tela/API, conforme `CONFIGURACAO.md`):
`VERSAO_BANCO`, `QUANTIDADE_EMPRESAS_PERMITIDAS`.
