# Distribuição DFe (DistribuicaoDfeService) — execução manual

Serviço (`DistribuicaoDfeService`, em
`AggilleDFe.Infrastructure/Integrations/DistribuicaoDfeService.cs`) que baixa
os XMLs de NFe e CTe de uma empresa via Distribuição DFe do SEFAZ (Zeus
DFe.NET), a partir do último NSU salvo (`Empresa.UltimoNsu`/`UltimoNsuCte`).
Processa **uma empresa por vez**; é chamado por três disparadores:

- Botão "Baixar XMLs" da tela de Empresas — `POST /api/v1/empresas/{id}/baixar-xmls`,
  `execucaoManual: true`, ignora a janela de horário.
- Botões "Baixar XMLs" do Home/menu lateral — todas as empresas elegíveis de
  uma vez, via `DistribuicaoLoteService` (`execucaoManual: true`, ignora a
  janela) — ver `DISTRIBUICAO_LOTE.md`.
- Ciclo automático do `AggilleDFe.Worker` — também via `DistribuicaoLoteService`,
  mas com `execucaoManual: false` (respeita `HoraInicial`/`HoraFinal` via
  `JanelaExecucaoService`) — ver `AggilleDFe.Worker/WORKER.md`.

Manifestação avulsa de uma NFe já baixada (fora do fluxo automático
resumo→ciência descrito abaixo) fica em `ManifestacaoService`, um serviço
separado — ver `MANIFESTACAO.md`.

## Configuração: NFe e CTe usam classes de configuração diferentes

`ZeusConfiguracaoFactory.Criar` (NFe) devolve `NFe.Utils.ConfiguracaoServico`.
CTe **não** usa essa mesma classe — confirmado por reflexão no construtor de
`CTe.Servicos.DistribuicaoDFe.ServicoCTeDistribuicaoDFe`, que exige
`CTe.Classes.ConfiguracaoServico` (tipo próprio do pacote `Zeus.Net.CTe`, sem
relação de herança com o da NFe — só compartilham
`DFe.Utils.ConfiguracaoCertificado` internamente). Por isso foi criado
`ZeusConfiguracaoFactory.CriarCte(empresa, diretorioSchemas)`, espelhando os
mesmos campos de `Empresa` (UF, ambiente, certificado, timeout, schemas), mas
retornando o tipo certo. O certificado (`X509Certificate2`) é carregado uma
única vez com `ZeusConfiguracaoFactory.CarregarCertificado` e reaproveitado
para os dois.

## Algoritmo (por modelo, laços independentes)

Cada modelo (NFe/CTe) mantém seu próprio cursor de NSU
(`Empresa.UltimoNsu`/`UltimoNsuCte`) e roda em laço:

1. Chama `NfeDistDFeInteresse`/`CTeDistDFeInteresse` com `ultNSU` = cursor atual.
2. `cStat == 137` (nenhum documento localizado) → fim do laço para esse modelo.
3. `cStat == 138` (documentos localizados) → processa cada item do lote
   (lotes de até 50 registros, conforme o manual da SEFAZ), atualiza
   `Empresa.UltimoNsu(Cte)` para `status.ultNSU` e persiste **a cada lote**
   (não só no fim — evita perder progresso se uma chamada seguinte falhar).
4. Qualquer outro `cStat` (ex.: 656 "Consumo Indevido") → loga o erro e para
   o laço, sem lançar exceção (evita loop infinito em cenário de erro).
5. Se `status.ultNSU >= status.maxNSU`, não há NSU novo além do lote atual →
   para. Senão continua com `ultNSU = status.ultNSU` e uma pequena espera
   (1,5s) entre chamadas, mesma cautela adotada no app de exemplo oficial do
   Zeus (`NFe.AppTeste.NetCore`/`CTe.AppTeste.NetCore`) para não disparar o
   erro 656.

Cada item processado (sucesso ou erro) gera uma linha em `LOGS`
(`Chave`/`XmlId` preenchidos quando aplicável); ao final de cada laço é
gravada uma linha de resumo com `QuantidadeXmls` e o intervalo
`HoraInicio`/`HoraFinal` do laço inteiro.

## Mapeamento schema → ação

**NFe** (`loteDistDFeInt` já vem tipado pela lib — um destes non-null por item):

| Propriedade não-nula | Significado | Ação |
|---|---|---|
| `ResNFe` | Resumo do documento | Grava/atualiza `Xml` (Situacao="Resumo"). Se `Empresa.Manifesta == "S"`, chama `RecepcaoEventoManifestacaoDestinatario` com `NFeTipoEvento.TeMdCienciaDaOperacao` (210210) — ver decisão abaixo. |
| `ResEvento` | Resumo de evento | Se `tpEvento == "110111"` (Cancelamento) e já existe `Xml` com essa chave, marca `Cancelada = "S"`. Senão só loga. |
| `NfeProc` | Documento completo (`NFe` + `protNFe`) | Descompacta (`Compressao.Unzip`), salva o arquivo na pasta (ver convenção abaixo) e grava/atualiza `Xml` com todos os campos. |
| `ProcEventoNFe` | Evento completo | Se `tpEvento == 110111`, marca `Cancelada`. Sempre loga. |

**CTe** (`loteDistDFeInt` do CTe **não** tem propriedades tipadas — só
`NSU`/`schema`/`XmlNfe` — confirmado por reflexão e pelo código-fonte oficial
do `ServicoCTeDistribuicaoDFe`). A ação é decidida pelo **início da string
XML descompactada** (`Compressao.Unzip(...).RemoverDeclaracaoXml()`):

| Conteúdo começa com | Ação |
|---|---|
| `<cteProc` | Documento completo — desserializa com `FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>`, salva o arquivo e grava/atualiza `Xml`. |
| `<procEventoCTe` | Evento completo — desserializa com `FuncoesXml.XmlStringParaClasse<CTe.Classes.Servicos.DistribuicaoDFe.Schemas.procEventoCTe>`; se `tpEvento == 110111`, marca `Cancelada`. Sempre loga. |
| Qualquer outro (ex.: `resCTe`/`resEvento`, resumo raro) | **Decisão confirmada com o usuário**: só registra um `Xml` mínimo (chave extraída via parsing genérico com `XDocument`, procurando o elemento `chCTe`) e loga — **sem manifestação automática de CTe**. Não existe um equivalente claro de "Ciência da Operação" para CTe nesta versão da lib; o evento disponível é "Prestação de serviço em desacordo" (`CTeTipoEvento.Desacordo`), que é uma decisão do usuário/operador, não algo a automatizar. |

## Decisões de negócio confirmadas com o usuário

- **Tipo de manifestação automática do destinatário (NFe)**: Ciência da
  Operação (`NFeTipoEvento.TeMdCienciaDaOperacao`, 210210) — é a única que não
  representa uma decisão definitiva e serve para destravar o download do XML
  completo na consulta seguinte (mesmo padrão do app de exemplo oficial do
  Zeus). Só é emitida se `Empresa.Manifesta == "S"`.
- **CTe em formato resumo**: apenas registra e loga (ver tabela acima) — sem
  manifestação automática de CTe.

## Convenção de pasta dos XMLs baixados

`{Empresa.PastaXml}/{Empresa.Cnpj}/{ano:D4}/{mes:D2}/{NFe|CTe}/{chave}.xml`
(ano/mês da data de emissão do documento, não da data de download), criando
os diretórios conforme necessário. Se `Empresa.PastaXml` estiver vazio, o
item falha (é tratado como erro por item, logado, sem interromper o restante
do lote nem os demais NSUs).

## Limitações conhecidas / fora de escopo

- NFSe: não há pacote Zeus.Net referenciado para NFSe nesta solution.
- Manifestação de CTe ("Prestação de serviço em desacordo"): não implementada
  (é uma decisão do usuário, não uma automação).
