# Manifestação do Destinatário sob demanda (ManifestacaoService)

Serviço (`ManifestacaoService`, em
`AggilleDFe.Infrastructure/Integrations/ManifestacaoService.cs`) que permite
manifestar uma NFe específica (pela chave de acesso) fora do fluxo automático
de resumo→ciência da rotina de download (ver `DISTRIBUICAO_DFE.md`). Usado
pelos botões da tela XMLs Baixados e pela API de integração `/api/v1/dfe`.

## Os 3 eventos suportados

Só NFe (`Xml.Modelo == "55"`) — CTe não tem manifestação do destinatário
equivalente no Zeus.Net nesta versão (mesma conclusão da pesquisa da rotina
de download, ver `DISTRIBUICAO_DFE.md`):

- **Ciência da Operação** (`NFeTipoEvento.TeMdCienciaDaOperacao`, 210210) —
  sem motivo. Grava `Xml.DataCiencia`.
- **Desconhecimento da Operação** (`NFeTipoEvento.TeMdDesconhecimentoDaOperacao`,
  210220) — motivo obrigatório. Grava `Xml.DataDesconhecimento`.
- **Operação Não Realizada** (`NFeTipoEvento.TeMdOperacaoNaoRealizada`,
  210240) — motivo obrigatório. Grava `Xml.DataNaoRealizacao` e
  `Xml.MotivoNaoRealizacao`.

Todos os três chamam o mesmo método do Zeus.Net,
`ServicosNFe.RecepcaoEventoManifestacaoDestinatario(idlote, sequenciaEvento,
chaveNFe, tipoEvento, cpfcnpj, justificativa, dhEvento)`, variando o
`NFeTipoEvento` e a `justificativa`.

## Validação do motivo (Desconhecimento / Operação Não Realizada)

15 a 255 caracteres, obrigatório — regra do campo `xJust` do schema oficial
do evento de manifestação do SEFAZ. Validado **antes** de chamar o SEFAZ
(evita gastar uma chamada real com um motivo inválido).

## De onde vem a empresa usada na manifestação

A chave sozinha não diz qual CNPJ está manifestando — por isso a
manifestação só funciona para chaves que já têm um registro em `XMLS` (a
`Empresa` é resolvida via `Xml.EmpresaId`, preenchido quando o XML foi
baixado/registrado pela rotina de download). Chave sem registro local →
"Chave não encontrada."

## Sucesso/erro

Sucesso quando o `cStat` do lote de evento retornado pelo SEFAZ é `128`
("Lote de Evento Processado" — mesmo critério usado em
`DistribuicaoDfeService` para a manifestação automática de Ciência). Qualquer
outro cenário (config inválida, falha de comunicação, `cStat` diferente) é
logado em `LOGS` (com `Chave`/`XmlId`) e devolvido como erro — o serviço
nunca lança exceção para quem chama, mesmo contrato de
`SefazStatusService`/`DistribuicaoDfeService`.

## Dois grupos de endpoints, mesmo serviço

- `POST /api/v1/dfe/{chave}/manifestacao/...` — protegido por Basic Auth
  (usuário/senha da tela de Configuração), para sistemas externos.
- `POST /api/v1/xmls/{chave}/manifestacao/...` — sem autenticação (mesmo
  modelo dos demais endpoints internos, confiança via CORS), usado pelos
  botões da tela XMLs Baixados.

A senha da API (`Configuracao.SenhaApi`) não pode ser embutida no cliente
Blazor WASM (código roda no navegador do usuário final, é público) — por
isso a tela usa o grupo sem autenticação, e não o `/api/v1/dfe` protegido.
Os dois grupos chamam exatamente o mesmo `IManifestacaoService`, sem
duplicação de lógica.
