# Definições da tabela USUARIO

# Nome da tabela: USUARIOS

# Campos:

- ID SERIAL PRIMARY KEY,
- LOGIN VARCHAR(50) NOT NULL, -- único, usado pra autenticar
- NOME VARCHAR(100),
- SENHA_HASH VARCHAR(1024) NOT NULL, -- hash gerado por PasswordHasher<Usuario> (Microsoft.Extensions.Identity.Core), nunca texto puro
- ADMINISTRADOR VARCHAR(1), -- S/N — controla só o acesso à tela de manutenção de Usuários, independente das permissões de módulo abaixo
- ACESSO_XMLS_BAIXADOS VARCHAR(1), -- S/N
- ACESSO_REGISTROS VARCHAR(1), -- S/N
- ACESSO_EMPRESAS VARCHAR(1), -- S/N
- ACESSO_CONFIGURACAO VARCHAR(1), -- S/N
- ACESSO_IMPORTACAO VARCHAR(1), -- S/N
- ACESSO_BAIXAR_XML VARCHAR(1), -- S/N
- ACESSO_EXPORTAR_XMLS VARCHAR(1), -- S/N — tela "Exportar XMLs" (zip de NFe/CTe do período), separada de ACESSO_BAIXAR_XML (que é o download via SEFAZ) e de ACESSO_XMLS_BAIXADOS (grid de visualização)
- ACESSO_BAIXAR_POR_CHAVE VARCHAR(1), -- S/N — tela "Baixar por Chave" (baixa uma NFe específica, sob demanda, ver `AggilleDFe.Infrastructure/Integrations/BAIXAR_POR_CHAVE.md`), separada de ACESSO_BAIXAR_XML (que dispara o ciclo completo por NSU de uma ou todas as empresas)
- INATIVO VARCHAR(1) -- S/N, mesmo padrão de EMPRESAS.INATIVO — desativa sem excluir

# Observações

- Usuário padrão `aggille` é semeado automaticamente pela API na primeira
  subida (`Program.cs`, só se a tabela estiver vazia), com
  `ADMINISTRADOR = 'S'` e as permissões de módulo em `'N'` — só enxerga a
  tela de manutenção de Usuários. Senha inicial `Ag1ll32017`, já em hash.
- O login feito pelo Blazor Web hoje só é validado no front-end (nenhuma tela
  do Blazor abre sem token válido) — os demais endpoints internos da API
  (Empresas/Configuração/Registros/XMLs/Dashboard) ainda não exigem esse
  token. Ver `AggilleDFe.Application/Services/AUTENTICACAO.md`.
