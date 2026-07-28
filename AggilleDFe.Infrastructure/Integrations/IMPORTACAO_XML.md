# Importação de XMLs de uma pasta (XmlImportService)

Serviço (`XmlImportService`, em
`AggilleDFe.Infrastructure/Integrations/XmlImportService.cs`) que varre uma
pasta informada pelo usuário (recursivamente) em busca de arquivos `.xml` de
NFe/CTe já autorizados e cria o registro em `XMLS` para as chaves que ainda
não existem — usado pela tela "Importar XMLs"
(`AggilleDFe.Web/Pages/ImportarXmls.razor`) via
`POST /api/v1/xmls/importar`.

## Algoritmo

1. Valida que a pasta existe; `Directory.EnumerateFiles(pasta, "*.xml",
   SearchOption.AllDirectories)` — varre subpastas também.
2. Para cada arquivo, remove a declaração XML (`RemoverDeclaracaoXml()`,
   mesma extensão usada em `DISTRIBUICAO_DFE.md` para CTe) e olha o início do
   conteúdo:
   - `<nfeProc` → desserializa com
     `FuncoesXml.XmlStringParaClasse<NFe.Classes.nfeProc>` e usa
     `protNFe.infProt.chNFe` como chave.
   - `<cteProc` → desserializa com
     `FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>` e usa
     `protCTe.infProt.chCTe` como chave.
   - Qualquer outro conteúdo (resumo, evento avulso, XML de outro tipo,
     `NFe`/`CTe` sem protocolo anexado) → contabilizado em
     `FormatoNaoReconhecido`, ignorado. **Decisão**: só se aceitam documentos
     já autorizados com protocolo (`nfeProc`/`cteProc`) — evita importar XML
     sem validade fiscal por engano.
3. Se já existe um `Xml` com essa chave → contabilizado em `JaExistiam`, e o
   registro existente é **atualizado** com `ConteudoXml` (e `NomeXml`, só se
   ainda estiver vazio — não sobrescreve um caminho de disco já válido)
   **decisão revista**: a versão anterior deste serviço só pulava sem
   atualizar nada; mudou porque muitos registros antigos (de antes do campo
   `ConteudoXml` existir, ou baixados só como resumo) precisavam de um jeito
   de backfill — reimportar a pasta agora serve pra isso.
4. Busca a `Empresa` pelo CNPJ do **destinatário** (`infNFe.dest.CNPJ`/
   `infCte.dest.CNPJ`), não do emitente — as notas são emitidas *para* a
   empresa cadastrada no sistema (ela é quem recebe/contrata, não quem
   emite), mesma lógica de quem é "dono" do documento na Distribuição DFe
   (ver `DISTRIBUICAO_DFE.md`). `IEmpresaRepository.ObterPorCnpjAsync`
   (novo método). Se o destinatário não tiver CNPJ (pessoa física, só CPF)
   ou não achar a empresa cadastrada → contabilizado em
   `EmpresaNaoEncontrada`, ignorado. O CNPJ do **emitente** continua sendo
   gravado em `Xml.FornecedorCnpj` (é o fornecedor/prestador, campo
   correto para isso).
5. Caso contrário, cria o `Xml` com os mesmos campos preenchidos pela rotina
   de download (`DISTRIBUICAO_DFE.md`) — fornecedor, valores, protocolo,
   número/série, modelo, `Situacao = "Documento completo (importado)"` (para
   diferenciar na tela de um documento baixado pela Distribuição DFe),
   `ConteudoXml` com o texto do arquivo lido (mesma lógica de "banco como
   fonte de verdade" da Distribuição DFe — ver `XMLS.md`).

## Decisão: os arquivos NÃO são copiados para a pasta padrão da empresa

Diferente da rotina de download (que salva em
`PastaXml/cnpj/ano/mes/NFe|CTe/chave.xml`), a importação grava `Xml.NomeXml`
apontando para o **caminho original** do arquivo encontrado — sem copiar ou
reorganizar. O pedido do usuário foi só "ler os xmls dessa pasta e criar o
registro no banco", sem mencionar mover os arquivos; copiar duplicaria
armazenamento e arriscaria conflito com arquivos que o usuário já organiza à
sua maneira. Como o conteúdo também vai para `Xml.ConteudoXml` (ver
`XMLS.md`), mover/apagar a pasta original depois de importar **não** quebra
mais "Baixar XML"/"Ver DANFE" (ambos preferem o conteúdo do banco); só o
botão "Salvar em disco" da tela XMLs Baixados depende de a empresa ter uma
`PastaXml` válida configurada.

## Sem gravação em LOGS

Diferente da rotina de download, a importação não grava linhas em `LOGS` —
`LOGS` é especificamente o registro de execução da Distribuição DFe (CLAUDE.md:
"Toda vez que o processo de baixar os xmls..."); importação é uma ação
administrativa avulsa e distinta. O resultado (quantos importados/ignorados/
com erro) é devolvido direto na resposta da API e mostrado na tela.

Ver `AggilleDFe.Application/DTOs/IMPORTAR_XMLS_DTO.md` para os campos do
resultado.
