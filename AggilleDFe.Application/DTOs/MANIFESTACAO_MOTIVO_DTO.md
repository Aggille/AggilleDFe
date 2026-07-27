# DTO de Motivo de Manifestação (ManifestacaoMotivoDto)

Corpo (JSON) esperado por `POST .../manifestacao/desconhecimento` e
`POST .../manifestacao/nao-realizada`, tanto no grupo interno
(`/api/v1/xmls/{chave}/manifestacao/...`) quanto no grupo de integração
protegido por Basic Auth (`/api/v1/dfe/{chave}/manifestacao/...`). Ver
`AggilleDFe.Infrastructure/Integrations/MANIFESTACAO.md`.

- Motivo: string — obrigatório, 15 a 255 caracteres (validado em
  `ManifestacaoService`, mesma regra do campo `xJust` do schema oficial do
  evento de manifestação do SEFAZ)

Não usado por `POST .../manifestacao/ciencia` (Ciência da Operação não tem
motivo).
