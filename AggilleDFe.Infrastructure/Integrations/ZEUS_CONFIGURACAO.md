# Configuração do Zeus DFe.NET (ZeusConfiguracaoFactory)

Fábrica estática (`ZeusConfiguracaoFactory`, em
`AggilleDFe.Infrastructure/Integrations/ZeusConfiguracaoFactory.cs`) que
constrói os objetos de configuração reais do pacote NuGet **Zeus.Net**
(`Zeus.Net.NFe.NFCe` / `Zeus.Net.CTe`, versão 2026.7.16.1250) a partir dos
campos da entidade `Empresa`.

A API real da lib foi levantada por **reflexão nas DLLs restauradas**
(`DFe.Classes.dll`, `DFe.Utils.dll`, `NFe.Utils.dll`, `NFe.Servicos.dll`), não
por suposição — os tipos/assinaturas abaixo foram confirmados dessa forma.

## Mapeamento Empresa → NFe.Utils.ConfiguracaoServico

| Campo em `Empresa` | Propriedade real do Zeus.Net | Observação |
|---|---|---|
| `CertificadoDigital` (caminho .pfx) | `ConfiguracaoServico.Certificado.Arquivo` (`DFe.Utils.ConfiguracaoCertificado`) | `TipoCertificado` fixado em `A1Arquivo` (certificado A1 em arquivo) |
| `SenhaCertificado` | `ConfiguracaoServico.Certificado.Senha` | |
| `Timeout` | `ConfiguracaoServico.TimeOut` | se nulo, usa `30000` (30s) como padrão |
| `Uf` | `ConfiguracaoServico.cUF` (enum `DFe.Classes.Entidades.Estado`) | `Enum.Parse` direto — os códigos de UF do DO já batem com os nomes do enum (`SP`, `MS`, etc.) |
| `Ambiente` (`"P"`/`"H"`) | `ConfiguracaoServico.tpAmb` (enum `DFe.Classes.Flags.TipoAmbiente`) | `"P"` → `Producao` (1), `"H"` → `Homologacao` (2) |
| — | `ConfiguracaoServico.ModeloDocumento` | fixado em `NFe` (55); a `Empresa` não distingue NFe/NFCe hoje |
| — | `ConfiguracaoServico.tpEmis` (enum `TipoEmissao`) | fixado em `teNormal` — obrigatório para a lib resolver a URL do serviço; sem isso o Zeus falha com "Serviço ... não disponível" mesmo com UF/ambiente corretos (erro real reproduzido durante o desenvolvimento) |
| — | `ConfiguracaoServico.DefineVersaoServicosAutomaticamente` | fixado em `false` — na prática, `true` não resolveu a versão do serviço sozinho (erro "versão , não disponível"); foi preciso setar `VersaoNfeStatusServico` explicitamente |
| — | `ConfiguracaoServico.VersaoNfeStatusServico` | fixado em `VersaoServico.Versao400` (layout 4.00, o vigente) |
| — | `ConfiguracaoServico.VersaoNFeDistribuicaoDFe` | fixado em `VersaoServico.Versao100` — é o **único** valor cadastrado para esse serviço na tabela de endereços do Zeus.Net (`NFe.Utils/Enderecos/Enderecador.cs`, serviço `NFeDistribuicaoDFe` só existe sob `versao1`/`Versao100`, tanto em homologação quanto produção, para todas as UFs — é um serviço do Ambiente Nacional, não por UF). Sem esse valor explícito (`DefineVersaoServicosAutomaticamente = false` e nenhum valor setado, ou seja, `default(VersaoServico)` = 0) a lib não acha nenhuma URL e lança `Exception("Serviço NFeDistribuicaoDFe, versão , não disponível para a UF ..., no ambiente de ..., para emissão tipo Normal, documento: NF-e!")` — erro real reproduzido durante o desenvolvimento, confirmado lendo o código-fonte oficial do `Enderecador.ObterUrlServico`/`Erro(...)` no GitHub |
| — | `ConfiguracaoServico.VersaoRecepcaoEventoManifestacaoDestinatario` | fixado em `VersaoServico.Versao400` — a manifestação do destinatário tem endereço cadastrado tanto em `Versao100` (wrapper legado `RecepcaoEvento`) quanto em `Versao400` (wrapper dedicado `RecepcaoEventoManifestacaoDestinatario4AN`, mesmo padrão AN do serviço acima); usamos a versão vigente, mas **também precisa estar setada explicitamente** pelo mesmo motivo — sem ela a manifestação de Ciência da Operação (ver `DISTRIBUICAO_DFE.md`) falharia com o mesmo tipo de erro |
| — | `ConfiguracaoServico.DiretorioSchemas` / `ValidarSchemas` | lidos de `configuration["SchemasPath"]` (padrão: pasta `SCHEMAS` relativa ao diretório de trabalho da API, mesma convenção do `CertificadosPath`). Se a pasta não existir, `ValidarSchemas` fica `false` automaticamente (funciona sem schemas, só sem validação local) |

Para carregar o certificado é usado
`DFe.Utils.CertificadoDigitalUtils.ObterDoCaminho(caminho, senha)`, que
retorna um `X509Certificate2` padrão do .NET (API cross-platform, não depende
de WinCrypt/Capicom).

## CTe usa uma classe de configuração diferente (`ZeusConfiguracaoFactory.CriarCte`)

`Criar` (acima) monta um `NFe.Utils.ConfiguracaoServico`, usado pelos
serviços de NFe (`NFe.Servicos.ServicosNFe`). O CTe **não aceita esse
mesmo tipo** — confirmado por reflexão no construtor de
`CTe.Servicos.DistribuicaoDFe.ServicoCTeDistribuicaoDFe`, que exige
`CTe.Classes.ConfiguracaoServico` (classe própria do pacote `Zeus.Net.CTe`,
sem relação de herança com a de NFe; só compartilham
`DFe.Utils.ConfiguracaoCertificado` internamente). `CriarCte(empresa,
diretorioSchemas)` espelha os mesmos campos de `Empresa` mapeados para esse
segundo tipo (`cUF`, `tpAmb`, `ConfiguracaoCertificado`, `TimeOut`,
`DiretorioSchemas`/`IsValidaSchemas`), fixando `TipoEmissao =
CTe.Classes.Informacoes.Tipos.tpEmis.teNormal` e `VersaoLayout =
CTe.Classes.Servicos.Tipos.versao.ve400` (mesmo raciocínio do `tpEmis`/
`VersaoNfeStatusServico` fixados para NFe, acima). Usado por
`DistribuicaoDfeService` — ver
`AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`.

## Campos de `Empresa` SEM correspondência no Zeus.Net

- **`TempoRetorno`, `IntervaloTentativas`, `QuantidadeTentativas`** — não
  existe nenhuma propriedade equivalente em `ConfiguracaoServico`. A lib não
  tem lógica de retry embutida nesse nível; esses campos ficam reservados
  para quando o `AggilleDFe.Worker` implementar o laço de retentativa nas
  consultas agendadas.
- **`SslLib`, `SslCrypt`, `SslHttpLib`, `SslXmlSignLib`, `SslType`** —
  **removidos** da entidade `Empresa`, do DTO e do banco (migration
  `RemoverCamposSslEmpresa`). Esses campos vieram da tela legada de cadastro,
  mas pertencem à versão Delphi/Object Pascal antiga do Zeus DFe — não existe
  nenhum tipo com esses nomes em `DFe.*`/`NFe.*` do pacote .NET (confirmado
  varrendo todos os assemblies por reflexão). O .NET moderno resolve TLS via
  a stack padrão do `HttpClient`/SChannel-OpenSSL do próprio SO.
- **`ServidorSmtp`/`UsuarioSmtp`/`SenhaSmtp`/`EmailSmtp`/`PortaSmtp`/
  `EmailEnvioNotificacoes`** — o Zeus.Net tem uma classe própria para envio
  de e-mail (`NFe.Utils.Email.ConfiguracaoEmail`), mas ela ainda não foi
  integrada; esses campos continuam existindo na entidade para uso futuro.
- **`ServidorPop`/`UsuarioPop`/`SenhaPop`/`EmailPop`/`PortaPop`** — não têm
  nenhuma classe correspondente no Zeus.Net (a lib não lê e-mails, só envia).
  São usados por uma funcionalidade própria do AggilleDFe fora do escopo do
  Zeus.

## Pasta de Schemas (SCHEMAS)

O Zeus.Net valida o XML (envio e retorno) contra XSDs locais quando
`ValidarSchemas = true`. Esses arquivos **não vêm no pacote NuGet** — é
preciso copiá-los manualmente.

- **Onde copiar**: pasta `SCHEMAS` na raiz do diretório de trabalho da API
  (`AggilleDFe.API/SCHEMAS/` em desenvolvimento — mesmo nível da pasta
  `CERTIFICADOS`). Configurável via chave `SchemasPath` no `appsettings.json`.
- **Estrutura esperada**: pasta **plana** — todos os `.xsd` soltos, sem
  subpastas por modelo (NFe/CTe/MDFe) nem por versão. Nome de exemplo:
  `consStatServ_v4.00.xsd` (o schema usado na consulta de status testada
  abaixo). Confirmado inspecionando a pasta `NFe.AppTeste/Schemas/` do
  próprio repositório oficial do Zeus DFe.NET no GitHub
  (github.com/ZeusAutomacao/DFe.NET) — é a mesma estrutura usada no app de
  demonstração da lib.
- **Fonte oficial dos schemas**: o pacote de schemas XSD é distribuído pelo
  Portal Nacional da NF-e (nfe.fazenda.gov.br, seção "Documentos Técnicos" /
  "Schemas XML"). Os arquivos do repositório de demonstração do Zeus servem
  como ponto de partida rápido para desenvolvimento, mas para produção vale
  conferir se estão na versão mais atual publicada pela SEFAZ.
- Enquanto a pasta não existir (ou `SchemasPath` não for configurado), o
  serviço funciona normalmente, só sem validação local do XML antes de
  enviar. Testado dos dois jeitos: sem a pasta `SCHEMAS` (`ValidarSchemas`
  cai para `false` automaticamente) e depois com os 202 arquivos `.xsd`
  copiados para `AggilleDFe.API/SCHEMAS/` (`ValidarSchemas = true`) — em
  ambos os casos a consulta de status contra o SEFAZ-MS real (homologação)
  retornou `cStat 107 - Servico em Operacao`.

## Uso: consulta de status do SEFAZ (primeiro caso de uso real)

`SefazStatusService` (mesma pasta) usa a fábrica para testar a configuração
de ponta a ponta:

```csharp
var certificado = ZeusConfiguracaoFactory.CarregarCertificado(empresa);
var configuracao = ZeusConfiguracaoFactory.Criar(empresa);
using var servicosNFe = new NFe.Servicos.ServicosNFe(configuracao, certificado);
var retorno = servicosNFe.NfeStatusServico(exceptionCompleta: false);
// retorno.Retorno (retConsStatServ): cStat, xMotivo, dhRecbto, tMed, versao, cUF, tpAmb
```

Exposto via `GET /api/v1/empresas/{id}/status-sefaz` (ver
`EMPRESA_DTO.md`/`STATUS_SEFAZ_DTO.md`), chamado pelo ícone "Verificar Status
do SEFAZ" na tela de Empresas.

Erros tratados (retornam 400 com `{ erro }`, não 500):
- Empresa não encontrada
- Certificado digital não configurado ou arquivo `.pfx` inexistente no disco
- `Uf`/`Ambiente` da empresa inválidos para os enums do Zeus.Net
- Qualquer falha na chamada SOAP em si (rede, certificado inválido/expirado,
  senha incorreta, etc.) — mensagem da exceção original é repassada
