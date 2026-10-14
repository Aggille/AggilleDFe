# Consulta de Situação na SEFAZ (cancelamento de XMLs)

`SituacaoSefazXmlService` (`ISituacaoSefazXmlService`) consulta na SEFAZ a
situação atual de documentos já registrados na tabela `XMLS` e grava os que
estiverem cancelados. Hoje é acionado pelo parâmetro `consultaSefaz=true` de
`GET /api/v1/dfe/xmls` (ver `AggilleDFe.Application/DTOs/PESQUISA_XMLS_EMPRESA.md`).

## Entrada

- `empresaId`: empresa cujo certificado digital (A1) é usado nas consultas.
- `chaves`: chaves de 44 dígitos (NFe modelo 55 ou CTe modelo 57). O chamador
  já envia só as que ainda não estão canceladas.

## Serviços Zeus DFe.NET

| Documento | Serviço | Método |
|---|---|---|
| NFe (55) | `NFe.Servicos.ServicosNFe` | `NfeConsultaProtocolo(chave)` → `retConsSitNFe` (versão 4.00 — `VersaoNfeConsultaProtocolo` no `ZeusConfiguracaoFactory.Criar`) |
| CTe (57) | `CTe.Servicos.ConsultaProtocolo.ConsultaProtcoloServico` | `ConsultaProtocoloV4(chave, config)` → `retConsSitCTe` |

A consulta protocolo é atendida pela SEFAZ autorizadora do **emitente**, então
o `cUF` da configuração é trocado pela UF dos 2 primeiros dígitos da chave
(a UF da empresa destinatária é usada só na distribuição DFe).

As chamadas SEFAZ rodam em paralelo (até 4 simultâneas, cada uma com sua
configuração/instância Zeus); a gravação no banco é sequencial depois (o
`DbContext` não é thread-safe).

## Regra de cancelamento

O documento é considerado cancelado quando:

1. `procEventoNFe`/`procEventoCTe` traz evento de cancelamento
   (`tpEvento` 110111, ou 110112 — cancelamento por substituição, NFe) com
   `retEvento.cStat` 135, 136 ou 155; **ou**
2. o `cStat` da situação do documento é 101, 151 ou 155.

Gravação na entidade `Xml`:

| Campo | Valor |
|---|---|
| `Cancelada` | `"S"` |
| `DataCancelamento` | data de registro do evento (`dhRegEvento`); sem evento, a data atual |
| `MotivoCancelamento` | justificativa do emitente (`xJust` do evento — NFe: `detEvento.xJust`; CTe: `evCancCTe.xJust`); na falta, `xEvento`/`xMotivo` |

`cStat` 100 (autorizado) e 110/301/302 (denegado) são situações válidas sem
cancelamento — nada é gravado. Qualquer outro `cStat` (ex.: 217 — não consta
na base, 656 — consumo indevido) é tratado como erro.

## Logs (`LOGS`)

- Uma linha por cancelamento registrado (`NFe:`/`CTe: cancelamento registrado
  (consulta de situação na SEFAZ).`, com chave e XmlId).
- Uma linha por erro de consulta (com a chave).
- Uma linha de resumo por execução: documentos consultados, cancelamentos
  registrados e erros (`QuantidadeXmls` = quantidade consultada).
- Falha de configuração (certificado ausente, UF/ambiente inválido) gera uma
  linha de log e encerra a consulta sem alterar nada.

Erros nunca são propagados ao chamador: a listagem continua sendo devolvida
com a situação que estiver gravada.
