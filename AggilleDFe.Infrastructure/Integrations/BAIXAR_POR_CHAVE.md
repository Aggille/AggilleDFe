# Baixar XML por Chave (DistribuicaoDfeService.BaixarPorChaveAsync)

Método (`BaixarPorChaveAsync`, em
`AggilleDFe.Infrastructure/Integrations/DistribuicaoDfeService.cs`, mesma
classe da Distribuição DFe automática — ver `DISTRIBUICAO_DFE.md`) que baixa
uma NFe específica pela chave de acesso, sob demanda — exposto em
`POST /api/v1/empresas/{id}/baixar-xml-por-chave` (corpo `{ "chave": "..." }`,
`BaixarPorChaveDto`), usado pela tela "Baixar por Chave"
(`AggilleDFe.Web/Pages/BaixarXmlPorChave.razor`), atrás da permissão
`AcessoBaixarPorChave` (ver `AggilleDFe.Domain/Entities/USUARIO.md`).

## Por que reaproveita o mesmo serviço SEFAZ da Distribuição DFe

O serviço `NfeDistDFeInteresse` (Zeus.Net, `NFe.Servicos.ServicosNFe`) — o
mesmo usado no ciclo automático por NSU (`ExecutarNfeAsync`) — aceita, além
de `ultNSU`/`nSU`, um parâmetro `chNFE`: passando a chave nesse parâmetro (e
`ultNSU`/`nSU` vazios), a SEFAZ retorna o(s) item(ns) da distribuição
relativos só a essa chave — no mesmo formato (`resNFe`/`nfeProc`/`resEvento`/
`procEventoNFe`) que a rotina automática já sabe processar. Por isso
`BaixarPorChaveAsync` reaproveita `ProcessarItemNfeAsync` sem nenhuma
alteração — mesmo upsert por chave (`XmlRepository.ObterPorChaveAsync`),
mesma gravação em disco na pasta padrão (`CaminhoXmlHelper`), mesmo registro
em `LOGS`.

**Não confirmado contra a SEFAZ real** (precisa de certificado/empresa
cadastrada válida para testar) — os parâmetros `ultNSU: ""`/`nSU: ""` foram
inferidos pela assinatura do método via reflexão
(`NfeDistDFeInteresse(String ufAutor, String documento, String ultNSU,
String nSU, String chNFE)`), não por teste real. Se a chamada falhar ou
retornar algo inesperado, é o primeiro lugar a olhar.

## Diferenças em relação ao ciclo automático

- **Não mexe em `Empresa.UltimoNsu`** — essa consulta é independente da
  janela incremental usada pelo ciclo automático; atualizar o NSU aqui
  arriscaria fazer o próximo ciclo automático reprocessar desde um ponto
  anterior sem necessidade.
- **Só NFe** — o usuário pediu especificamente "chave de NFe"; não existe
  (ainda) equivalente pra CTe nesta tela.
- **Precisa de uma empresa cadastrada** — a consulta é feita com o
  certificado/UF/CNPJ de uma empresa já cadastrada no sistema (ela precisa
  ser a destinatária da NFe; a SEFAZ retorna `cStat 137` - "nenhum documento
  localizado" - se não for).

## Retorno (`ResultadoBaixarPorChaveDto`)

- `Encontrado: bool` — `false` se a SEFAZ não achou nada pra essa
  chave/empresa (`cStat 137`).
- `JaExistia: bool` — se já havia um registro `Xml` com essa chave antes
  dessa chamada (consultado antes de processar, pra informar corretamente
  mesmo com o upsert).
- `Mensagem: string` — descreve o resultado: XML baixado com sucesso,
  documento localizado mas só como resumo por enquanto (documento completo
  ainda não disponível), ou nada encontrado.

Erros de configuração (empresa não encontrada, chave com formato inválido,
certificado ausente/inválido, falha na chamada SOAP) voltam como
`Results.BadRequest(new { erro })`, mesmo padrão dos outros endpoints da API.

## Fallback automático no endpoint externo `GET /api/v1/dfe/{chave}/xml`

Esse endpoint (`DfeEndpoints.cs`, API externa autenticada com Basic Auth) usa
esse mesmo método como fallback: se `XmlArquivoService.ObterXmlBrutoAsync`
não achar a chave no banco/disco, em vez de devolver 404 na hora, o endpoint
tenta baixar da SEFAZ primeiro — chamando `BaixarPorChaveAsync` pra **cada
empresa cadastrada ativa** (não bloqueada por consumo indevido), na ordem em
que `IEmpresaRepository.PesquisarAsync` devolve, parando na primeira que
retornar `DocumentoCompletoBaixado = true`. Só devolve 404 se nenhuma
empresa conseguir.

**Por que testar todas as empresas**: a chave de acesso da NFe embute o CNPJ
do **emitente**, não do destinatário — e as `Empresa` cadastradas no
AggilleDFe são as destinatárias (quem recebe/baixa suas próprias notas). Não
dá pra saber de antemão, só pela chave, qual empresa cadastrada é a
destinatária certa; por isso o fallback varre todas.

**Custo**: cada empresa testada é uma chamada SOAP real à SEFAZ, então o
tempo de resposta desse endpoint cresce com o número de empresas cadastradas
quando a chave não está em cache local — só acontece no caminho de erro
(quando o XML ainda não foi baixado), não no caso comum (já baixado, resposta
imediata do banco).
