# Tela de configuração

# Tela de configuração global do sistema

# Somente 1 registro por banco de dados

# Classe D.O. ( CONFIGURACAO )

- Campos:
  - C.N.P.J.: campo CNPJ_EMPRESA - formatado com máscara de cnpj alfanumérico, com ícone de busca que consulta `GET /api/v1/empresas/consulta-cnpj/{cnpj}` e pré-preenche a Razão Social
  - Razão Social: campo NOME_EMPRESA
  - Tempo de Execução ( em minutos ) : Campo TEMPO_EXECUCAO INTEGER,
  - Porta da API: Campo PORTA_API INTEGER,
  - Usuários da API: campo USUARIO_API
  - Senha da API: campo SENHA_API
  - API Ativa: stitch campo API_ATIVA
  - Processar uma Empresa de cada vez: switch campo PROCESSAR_INDIVIDUALMENTE

<!-- 
    - VERSAO_BANCO INTEGER,    
    - QUANTIDADE_EMPRESAS_PERMITIDAS INTEGER, 
-->d
