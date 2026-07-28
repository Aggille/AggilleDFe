# Definições da class XML

# Nome da tabela: XMLS

# Campos:

- ID INTEGER NOT NULL,
- CHAVE VARCHAR(44) NOT NULL,
- PROTOCOLO VARCHAR(30),
- EMISSAO DATE,
- DATA_DOWNLOAD DATE,
- FORNECEDOR_NOME VARCHAR(100),
- FORNECEDOR_CNPJ VARCHAR(20),
- FORNECEDOR_CIDADE VARCHAR(100),
- FORNECEDOR_UF VARCHAR(2),
- VALOR_TOTAL NUMERIC(15,2),
- VALOR_ICMS NUMERIC(15,2),
- STATUS_NFE INTEGER,
- MENSAGEM_NFE VARCHAR(254),
- NOME_XML VARCHAR(1024),
- NUMERO INTEGER,
- SERIE VARCHAR(3),
- MODELO VARCHAR(3),
- EMPRESA_ID INTEGER,
- CANCELADA VARCHAR(1),
- SCHEMA VARCHAR(20),
- DESCRICAO VARCHAR(100),
- MENSAGEM VARCHAR(100),
- SITUACAO VARCHAR(100),
- DATA_CIENCIA DATE,
- DATA_REALIZACAO DATE,
- DATA_NAO_REALIZACAO DATE,
- DATA_DESCONHECIMENTO DATE,
- MOTIVO_NAO_REALIZACAO VARCHAR(1024),
- DATA_CANCELAMENTO DATE,
- MOTIVO_CANCELAMENTO VARCHAR(500),
- CONTEUDO_XML TEXT

# CONTEUDO_XML

Conteúdo bruto do XML (documento completo, `nfeProc`/`cteProc`) gravado no
banco no momento do download/importação — fonte de verdade independente do
arquivo em disco. Preenchido em paralelo à tentativa de gravação em
`Empresa.PastaXml`; se a gravação em disco falhar (permissão, caminho
inválido, pasta errada etc.), o registro em `CONTEUDO_XML` garante que o XML
não é perdido, e a tela "XMLS Baixados" oferece uma ação para regravar em
disco a partir dele. Toda leitura do conteúdo do XML para qualquer operação
(baixar arquivo, gerar DANFE/DACTE) deve preferir este campo e só cair para o
arquivo em disco (`NOME_XML`) se ele estiver vazio (registros antigos,
gravados antes deste campo existir).
