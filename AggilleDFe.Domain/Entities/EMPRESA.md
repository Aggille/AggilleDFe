# Definições da tabela EMPRESA

# Nome da tabela: EMPRESAS

# Campos:

- ID INTEGER NOT NULL,PRIMARY KEY
- RAZAO_SOCIAL VARCHAR(60),
- CNPJ VARCHAR(20),
- UF VARCHAR(2),
- CERTIFICADO_DIGITAL VARCHAR(1024),
- SENHA_CERTIFICADO VARCHAR(50),
- PASTA_XML VARCHAR(1024),
- ULTIMO_NSU INTEGER,
- AMBIENTE VARCHAR(1),
- TIMEOUT INTEGER,
- TEMPO_RETORNO INTEGER,
- INTERVALO_TENTATIVAS INTEGER,
- QUANTIDADE_TENTATIVAS INTEGER,
- EMAIL_ENVIO_NOTIFICACOES VARCHAR(1024),
- SERVIDOR_SMTP VARCHAR(200),
- USUARIO_SMTP VARCHAR(50),
- SENHA_SMTP VARCHAR(20),
- EMAIL_SMTP VARCHAR(200),
- TIPO_AUTENTICACAO_SMTP INTEGER,
- SERVIDOR_POP VARCHAR(200),
- USUARIO_POP VARCHAR(50),
- EMAIL_POP VARCHAR(200),
- SENHA_POP VARCHAR(20),
- TIPO_AUTENTICACAO_POP INTEGER,
- PORTA_POP INTEGER,
- PORTA_SMTP INTEGER,
- IE VARCHAR(20),
- MANIFESTA VARCHAR(1),
- POSICAO INTEGER,
- INATIVO VARCHAR(1),
- ULTIMO_NSU_CTE INTEGER,
- HORA_INICIAL TIME,
- HORA_FINAL TIME,
- BLOQUEADA_ATE TIMESTAMP,
- CERTIFICADO_NOTIFICADO_EM DATE

- HORA_INICIAL/HORA_FINAL: janela de horário em que o Worker pode baixar os
  XMLs dessa empresa automaticamente. Se ambos estiverem preenchidos, o
  download automático (agendado) só ocorre com a hora atual dentro da janela;
  se algum estiver vazio, não há restrição. Execução manual (ex.: botão
  "Baixar XMLs" na tela de Empresas) sempre ignora essa janela. Ver
  `AggilleDFe.Application/Services/JANELA_EXECUCAO.md`.
- BLOQUEADA_ATE: preenchido automaticamente quando a SEFAZ rejeita a
  distribuição com cStat 656 ("Consumo Indevido") — a empresa fica de fora
  de qualquer execução (automática ou manual em lote) até esse instante.
  Não editável na tela; ver `DISTRIBUICAO_DFE.md` e `DISTRIBUICAO_LOTE.md`.
- CERTIFICADO_NOTIFICADO_EM: data em que a última notificação de "certificado
  próximo do vencimento" foi enviada, pra não reenviar o e-mail em todo
  ciclo. Não editável na tela. Ver `DISTRIBUICAO_DFE.md`.

# Observações

- Os campos SSL_LIB, SSL_CRYPT, SSL_HTTP_LIB, SSL_XML_SIGN_LIB e SSL_TYPE
  (presentes na tela legada de cadastro de empresa) foram removidos: são
  configurações da versão Delphi/Object Pascal antiga do Zeus DFe e não têm
  correspondência em nenhuma classe do pacote NuGet Zeus.Net (`DFe.*`/`NFe.*`),
  conforme inspeção por reflexão das DLLs referenciadas
  (`Zeus.Net.NFe.NFCe`/`Zeus.Net.CTe` 2026.7.16.1250). Ver `ZEUS_CONFIGURACAO.md`
  em `AggilleDFe.Infrastructure/Integrations`.
