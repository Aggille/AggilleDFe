# DTOs de Importação de XMLs (ImportarXmlsDto / ResultadoImportacaoXmlsDto)

Usados por `POST /api/v1/xmls/importar` — ver
`AggilleDFe.Infrastructure/Integrations/IMPORTACAO_XML.md` para o algoritmo
completo.

## ImportarXmlsDto (corpo da requisição)

- Pasta: string — caminho completo, no servidor onde a API roda, de uma
  pasta a ser varrida recursivamente em busca de arquivos `.xml`

## ResultadoImportacaoXmlsDto (retorno)

- ArquivosEncontrados: int — total de arquivos `.xml` encontrados na pasta
  (recursivo)
- Importados: int — quantos viraram registro novo em `XMLS`
- JaExistiam: int — quantos já tinham um `Xml` com a mesma chave (o registro
  é atualizado com `ConteudoXml` e regravado na pasta padrão da empresa, não
  é pulado)
- EmpresaNaoEncontrada: int — quantos tinham CNPJ do emitente sem
  correspondência em `EMPRESAS` (ignorados)
- FormatoNaoReconhecido: int — quantos não eram `nfeProc`/`cteProc` (ex.:
  resumo, evento avulso, XML de outro tipo, arquivo corrompido) — ignorados
- Erros: string[] — mensagens de erro por arquivo, quando aplicável (nome do
  arquivo + motivo — parse do XML, ou falha ao gravar na pasta padrão da
  empresa; nesse último caso o registro no banco é feito normalmente)
- Mensagem: string? — resumo textual, exibido na tela
