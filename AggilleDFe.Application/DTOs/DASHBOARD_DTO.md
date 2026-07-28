# DTO do Dashboard (DashboardDto / DashboardEmpresaResumoDto)

Retorno de `GET /api/v1/dashboard`, usado pela seção de resumo da tela Home
(`AggilleDFe.Web/Pages/Home.razor`). Montado por `IDashboardService`/
`DashboardService` (`AggilleDFe.Infrastructure/Integrations/DashboardService.cs`),
só leitura — não altera nada no banco.

## DashboardDto

- EmpresasAtivas: int — quantas empresas têm `Inativo != "S"`
- EmpresasBloqueadas: int — quantas dessas estão com `Empresa.BloqueadaAte`
  no futuro (bloqueio por consumo indevido, cStat 656 — ver
  `AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`)
- CertificadosVencendoEm15Dias: int — quantas empresas ativas têm certificado
  digital carregável com 15 dias ou menos para vencer (ou já vencido)
- ErrosHoje: int — contagem heurística de linhas de `LOGS` de hoje cuja
  `Mensagem` contém "erro", "falha", "rejei" ou "inesperado" (case-insensitive)
  — não é um flag dedicado no banco, é busca por palavra-chave na mensagem
- Empresas: lista de `DashboardEmpresaResumoDto`, uma por empresa ativa

## DashboardEmpresaResumoDto

- EmpresaId / RazaoSocial
- UltimaExecucaoData / UltimaExecucaoHora: data/hora do último registro em
  `LOGS` **de hoje** para essa empresa (`null` se não rodou hoje ainda)
- Bloqueada: bool — `Empresa.BloqueadaAte > DateTime.Now`
- BloqueadaAte: DateTime? — repetido do domínio, pra montar a mensagem na tela
- CertificadoDiasRestantes: int? — dias até `X509Certificate2.NotAfter`
  (negativo se já venceu); `null` se o certificado não pôde ser carregado
  (caminho vazio, arquivo ausente, senha errada) — nesse caso a tela não deve
  tratar como "vencendo", só omitir a informação
