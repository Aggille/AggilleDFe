# Definições da tabela CONFIGURACAO

# Nome da tabela: CONFIGURACAO

# Campos:

- ID INTEGER NOT NULL,PRIMARY KEY
- NOME_EMPRESA VARCHAR(60),
- CNPJ_EMPRESA VARCHAR(14),
- VERSAO_BANCO INTEGER,
- TEMPO_EXECUCAO INTEGER,
- QUANTIDADE_EMPRESAS_PERMITIDAS INTEGER,
- API_ATIVA VARCHAR(1),
- PORTA_API INTEGER,
- USUARIO_API VARCHAR(50),
- SENHA_API VARCHAR(20),
- PROCESSAR_INDIVIDUALMENTE VARCHAR(1)
- ULTIMA_EMPRESA_PROCESSADA_ID INTEGER (Id da última empresa processada no
  ciclo automático, quando PROCESSAR_INDIVIDUALMENTE = "S" — usado pelo
  round-robin do DistribuicaoLoteService, ver DISTRIBUICAO_LOTE.md;
  gerenciado internamente pelo Worker, não é exposto/editável na tela de
  Configuração)
