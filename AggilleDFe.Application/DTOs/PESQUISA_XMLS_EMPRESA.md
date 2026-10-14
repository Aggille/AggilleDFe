# Pesquisa de XMLs por Empresa (`GET /api/v1/dfe/xmls`)

Endpoint da API externa de integração que devolve a lista dos XMLs baixados
para uma empresa cadastrada, filtrando por período e tipo de documento. É a
forma de um sistema integrador (ERP, contabilidade etc.) descobrir quais
documentos existem e então baixar cada arquivo por chave em
`GET /api/v1/dfe/{chave}/xml`.

## Camadas

| Camada | Arquivo | Responsabilidade |
|---|---|---|
| API | `AggilleDFe.API/Endpoints/DfeEndpoints.cs` | Rota, documentação Swagger, `200`/`400` |
| Application | `AggilleDFe.Application/Services/XmlService.cs` (`PesquisarPorEmpresaAsync`) | Validação dos parâmetros, localização da empresa pelo CNPJ, conversão do tipo em modelo |
| Domain/Infra | `IEmpresaRepository.ObterPorCnpjAsync`, `IXmlRepository.PesquisarAsync` | Consultas EF Core já existentes, reaproveitadas sem alteração |

Não há SQL manual nem DTO novo: o retorno é o `XmlDto` (ver `XML_DTO.md`).

## Autenticação

Mesma de todo o grupo `/api/v1/dfe/*`: **HTTP Basic** (política `BasicApi`,
`AggilleDFe.API/Auth/BasicApiAuthenticationHandler.cs`), com o usuário e a
senha definidos na tela Configuração (`CONFIGURACAO.USUARIO_API` /
`SENHA_API`). A API precisa estar ativa (`CONFIGURACAO.API_ATIVA = "S"`).
Credenciais ausentes/inválidas ou API desativada → `401`.

No Swagger (`/swagger`), as rotas desse grupo anunciam o esquema `Basic`, então
dá para testar pelo botão **Authorize** informando usuário e senha da API.

## Parâmetros (query string)

| Parâmetro | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `dataInicial` | date (`yyyy-MM-dd`) | sim | Início do período, inclusivo |
| `dataFinal` | date (`yyyy-MM-dd`) | sim | Fim do período, inclusivo; não pode ser menor que `dataInicial` |
| `cnpjEmpresa` | string | sim | CNPJ da **empresa cadastrada** (destinatária dos XMLs — não o fornecedor/emitente) |
| `tipo` | string | não (padrão `todos`) | `todos`, `nfe`, `cte` ou `nfse` — sem diferenciar maiúsculas |
| `usarDataDownload` | bool | não (padrão `false`) | `false`: período aplicado à data de **emissão**; `true`: à data de **download** (mesma convenção da exportação de XMLs) |
| `consultaSefaz` | bool | não (padrão `false`) | `false`: devolve a situação já gravada no banco; `true`: antes de responder, consulta na SEFAZ a situação dos documentos e grava os cancelamentos (ver abaixo) |

### `consultaSefaz`

Serve para o sistema integrador receber a lista com a situação de
cancelamento já atualizada — saber se a nota/conhecimento **está cancelado**
(`cancelada`) e **por quê** (`motivoCancelamento`, além de
`dataCancelamento`).

Com `consultaSefaz=true`:

1. A lista do período é montada normalmente.
2. Para cada documento **ainda não cancelado** (NFe modelo 55 / CTe modelo 57,
   chave de 44 dígitos), é feita a consulta protocolo na SEFAZ
   (`ISituacaoSefazXmlService`, implementado em
   `AggilleDFe.Infrastructure/Integrations/SituacaoSefazXmlService.cs` — ver
   `SITUACAO_SEFAZ_XML.md`), com o certificado da empresa.
3. Os cancelamentos encontrados são gravados na tabela `XMLS`
   (`CANCELADA = "S"`, `DATA_CANCELAMENTO`, `MOTIVO_CANCELAMENTO` = justificativa
   informada pelo emitente no evento de cancelamento).
4. A lista é recarregada do banco e devolvida já com esses campos atualizados.

Observações:

- Documentos já cancelados não são consultados de novo (cancelamento é
  definitivo).
- A resposta fica mais lenta (uma consulta SEFAZ por documento, até 4 em
  paralelo) — use períodos curtos; consultas repetidas da mesma chave em curto
  intervalo podem gerar rejeição de consumo indevido (cStat 656) da SEFAZ.
- Falhas na consulta (certificado, SEFAZ fora, chave não encontrada etc.)
  **não** mudam o status `200`: a lista volta com a situação que já estava
  gravada e o erro fica registrado em LOGS. Cada consulta gera uma linha de
  resumo em LOGS (consultados / cancelamentos registrados / erros).

### `cnpjEmpresa`

- Aceito com ou sem máscara (`12345678000195` ou `12.345.678/0001-95`): o
  service mantém só letras e dígitos e converte para maiúsculas antes de
  validar, que é o formato gravado no cadastro de empresas.
- Validado pelo `CnpjValidator` (formato de 14 caracteres + dígitos
  verificadores; aceita o CNPJ alfanumérico).
- Com máscara na URL, a `/` deve ir codificada (`%2F`); sem máscara isso não é
  necessário — formato recomendado.
- A empresa é localizada por `IEmpresaRepository.ObterPorCnpjAsync` e os XMLs
  são filtrados pelo `EmpresaId` dela. Empresas inativas/bloqueadas também são
  encontradas: o filtro é sobre o que já foi baixado, não sobre o ciclo de
  distribuição.

### `tipo`

| Valor | Filtro aplicado |
|---|---|
| `todos` | Nenhum filtro de modelo |
| `nfe` | `Modelo = "55"` |
| `cte` | `Modelo = "57"` |
| `nfse` | **Reservado.** A distribuição DFe da SEFAZ só entrega NFe/CTe e a NFSe ainda não é baixada nem gravada na tabela `XMLS`; por isso o valor já é aceito pela API (o contrato não muda quando a NFSe for implementada), mas hoje sempre devolve `200 []`. Ao implementar a NFSe, basta trocar o `return ([], null)` do `case "nfse"` em `XmlService.PesquisarPorEmpresaAsync` pelo modelo correspondente. |

Qualquer outro valor → `400`.

## Respostas

| Status | Quando | Corpo |
|---|---|---|
| `200` | Consulta válida | Array de `XmlDto` (pode ser vazio `[]`) |
| `400` | Datas ausentes, `dataInicial > dataFinal`, CNPJ inválido, CNPJ sem empresa cadastrada ou `tipo` inválido | `{ "erro": "<mensagem>" }` |
| `401` | Credenciais Basic ausentes/inválidas ou API desativada | — (header `WWW-Authenticate: Basic`) |

Mensagens de erro (`400`):

- `Informe a data inicial e a data final.`
- `A data inicial não pode ser maior que a data final.`
- `CNPJ da empresa inválido.`
- `Nenhuma empresa cadastrada com o CNPJ <cnpj>.`
- `Tipo de documento inválido — use todos, nfe, cte ou nfse.`

### Conteúdo da lista

Cada item é um `XmlDto` (campos em `XML_DTO.md`): chave, protocolo, emissão,
data de download, dados do fornecedor (nome, CNPJ, cidade, UF), valores
total/ICMS, número, série, modelo, situação, cancelamento etc. **Não inclui o
conteúdo do XML** — a consulta projeta sem `CONTEUDO_XML` (mesma projeção da
tela "XMLS Baixados"), para a resposta continuar leve com muitos documentos.
Para obter o arquivo, chamar `GET /api/v1/dfe/{chave}/xml` com a `chave` de
cada item.

Ordenação: data de download decrescente, depois emissão decrescente, depois
`Id` decrescente (mais recentes primeiro).

## Exemplos

Todas as NFe de setembro/2026 (por emissão) da empresa `12345678000195`:

```http
GET /api/v1/dfe/xmls?dataInicial=2026-09-01&dataFinal=2026-09-30&cnpjEmpresa=12345678000195&tipo=nfe
Authorization: Basic <base64 de usuario:senha>
```

```bash
curl -u usuario:senha "https://<servidor>/api/v1/dfe/xmls?dataInicial=2026-09-01&dataFinal=2026-09-30&cnpjEmpresa=12345678000195&tipo=todos&usarDataDownload=true"
```

Resposta `200` (resumida):

```json
[
  {
    "id": 1520,
    "chave": "35260998765432000110550010000012341000012345",
    "emissao": "2026-09-28",
    "dataDownload": "2026-09-29",
    "fornecedorNome": "FORNECEDOR EXEMPLO LTDA",
    "fornecedorCnpj": "98765432000110",
    "valorTotal": 1530.40,
    "numero": 1234,
    "serie": "1",
    "modelo": "55",
    "empresaId": 3,
    "cancelada": false
  }
]
```

Resposta `400`:

```json
{ "erro": "Nenhuma empresa cadastrada com o CNPJ 12345678000195." }
```
