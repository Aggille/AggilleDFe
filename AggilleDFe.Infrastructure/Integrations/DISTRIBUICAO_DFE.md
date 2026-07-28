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

**Decisão confirmada com o usuário sobre o que vira log**: `LOGS` só registra
XML baixado (documento completo, com ou sem sucesso ao gravar em disco),
resumo da consulta (`ResNFe`/resumo genérico de CTe) e erros (cStat
inesperado, exceção ao processar item, falha de manifestação). Eventos que
não são cancelamento (`ResEvento`/`ProcEventoNFe`/`procEventoCTe` sem
`tpEvento == 110111`) **não geram log** — só atualizam `Xml` quando aplicável
(cancelamento sempre loga, por ser uma mudança de estado relevante). `Chave`/
`XmlId` são preenchidos quando aplicável, `Nsu` sempre preenchido com o NSU
do item/consulta que originou a linha; ao final de cada laço é gravada uma
linha de resumo com `QuantidadeXmls`, o intervalo `HoraInicio`/`HoraFinal` do
laço inteiro e o `Nsu` (último NSU alcançado, igual ao
`UltimoNsu`/`UltimoNsuCte` salvo na empresa). As linhas geradas por
`ManifestacaoService` (manifestação avulsa, fora do laço de distribuição)
não têm `Nsu` — não há um NSU em escopo nesse fluxo, que é acionado por
`Chave` diretamente.

## Mapeamento schema → ação

**NFe** (`loteDistDFeInt` já vem tipado pela lib — um destes non-null por item):

| Propriedade não-nula | Significado | Ação |
|---|---|---|
| `ResNFe` | Resumo do documento | Grava/atualiza `Xml` (Situacao="Resumo") e **loga** (resumo da consulta). Se `Empresa.Manifesta == "S"`, chama `RecepcaoEventoManifestacaoDestinatario` com `NFeTipoEvento.TeMdCienciaDaOperacao` (210210) — ver decisão abaixo. |
| `ResEvento` | Resumo de evento | Se `tpEvento == "110111"` (Cancelamento) e já existe `Xml` com essa chave, marca `Cancelada = "S"` e loga. Senão, **não loga** (evento que não é cancelamento). |
| `NfeProc` | Documento completo (`NFe` + `protNFe`) | Descompacta (`Compressao.Unzip`), tenta salvar o arquivo na pasta (ver convenção abaixo) — falha na gravação não impede o registro no banco, ver seção "Banco de dados como fonte de verdade" — e grava/atualiza `Xml` com todos os campos. **Loga** (XML baixado, com ou sem sucesso ao gravar em disco). |
| `ProcEventoNFe` | Evento completo | Se `tpEvento == 110111`, marca `Cancelada` e loga. Senão, **não loga**. |

**CTe** (`loteDistDFeInt` do CTe **não** tem propriedades tipadas — só
`NSU`/`schema`/`XmlNfe` — confirmado por reflexão e pelo código-fonte oficial
do `ServicoCTeDistribuicaoDFe`). A ação é decidida pelo **início da string
XML descompactada** (`Compressao.Unzip(...).RemoverDeclaracaoXml()`):

| Conteúdo começa com | Ação |
|---|---|
| `<cteProc` | Documento completo — desserializa com `FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>`, tenta salvar o arquivo e grava/atualiza `Xml`. **Loga** (XML baixado, com ou sem sucesso ao gravar em disco). |
| `<procEventoCTe` | Evento completo — desserializa com `FuncoesXml.XmlStringParaClasse<CTe.Classes.Servicos.DistribuicaoDFe.Schemas.procEventoCTe>`; se `tpEvento == 110111`, marca `Cancelada` e loga. Senão, **não loga**. |
| Qualquer outro (ex.: `resCTe`/`resEvento`, resumo raro) | **Decisão confirmada com o usuário**: só registra um `Xml` mínimo (chave extraída via parsing genérico com `XDocument`, procurando o elemento `chCTe`) e **loga** (resumo da consulta) — **sem manifestação automática de CTe**. Não existe um equivalente claro de "Ciência da Operação" para CTe nesta versão da lib; o evento disponível é "Prestação de serviço em desacordo" (`CTeTipoEvento.Desacordo`), que é uma decisão do usuário/operador, não algo a automatizar. |

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
os diretórios conforme necessário — convenção compartilhada em
`AggilleDFe.Infrastructure/Storage/CaminhoXmlHelper.cs`.

## Banco de dados como fonte de verdade (`Xml.ConteudoXml`)

A gravação em disco é **best effort**: o conteúdo do XML completo
(`nfeProc`/`cteProc`) é sempre gravado em `Xml.ConteudoXml` (ver `XMLS.md`),
independente do resultado da gravação em disco. Se `Empresa.PastaXml`
estiver vazio ou a gravação falhar por qualquer motivo (permissão, caminho
errado, disco cheio), o item **não falha mais** — o registro em `XMLS` é
persistido normalmente com o conteúdo, `NomeXml` fica com o valor anterior
(ou nulo, se novo), e é gravada uma linha em `LOGS` com a mensagem específica
"XML baixado e registrado no banco, mas falhou ao gravar em disco: {erro}"
(em vez do erro genérico "erro ao processar NSU X"). A tela "XMLS Baixados"
tem uma ação "Salvar em disco" (`POST /api/v1/xmls/{chave}/salvar-em-disco`,
`XmlArquivoService.SalvarEmDiscoAsync`) que regrava o arquivo a partir do
conteúdo já no banco — útil quando a pasta configurada estava errada e foi
corrigida depois. Qualquer leitura do XML para outra operação (baixar
arquivo, DANFE) também prefere `ConteudoXml` e só cai para o arquivo em disco
se ele estiver vazio (registros antigos).

## Limitações conhecidas / fora de escopo

- NFSe: não há pacote Zeus.Net referenciado para NFSe nesta solution.
- Manifestação de CTe ("Prestação de serviço em desacordo"): não implementada
  (é uma decisão do usuário, não uma automação).
