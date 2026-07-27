# Tela de Importar XMLs

# Tela para importar XMLs de NFe/CTe já existentes em disco para dentro do banco (tabela XMLS)

# Conteúdo da tela:

- Um campo de texto (Pasta) com o caminho completo, **no servidor onde a API
  roda**, de uma pasta a ser varrida — e um botão Importar.
- `POST /api/v1/xmls/importar` com `{ Pasta }` (ver
  `AggilleDFe.Application/DTOs/IMPORTAR_XMLS_DTO.md` e
  `AggilleDFe.Infrastructure/Integrations/IMPORTACAO_XML.md` para o
  algoritmo completo) — usa um `HttpClient` próprio com timeout de 30 min
  (mesmo padrão de `BaixarXmls.razor`), já que varrer muitos arquivos pode
  demorar.
- Mostra o resumo (`ResultadoImportacaoXmlsDto`) num `MudAlert` e, se houver
  erros por arquivo, lista cada um (`MudList`).
- Acessível pelo menu lateral ("Importar XMLs").
- **Não sobrescreve** XMLs já cadastrados (mesma chave) e associa cada
  documento pela empresa com o mesmo CNPJ do **destinatário** do XML (a
  empresa cadastrada no sistema é sempre quem recebe o documento, não quem
  emite) — se não achar a empresa, o arquivo é ignorado (contabilizado em
  "sem empresa correspondente", não gera erro).
