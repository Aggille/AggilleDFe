# Tela de Usuários

# Tela que exibe os usuários cadastrados e permite o CRUD das entidades

# Classe D.O. USUARIO

# Só acessível por quem tem `Administrador = true` (`[Authorize(Policy = "Administrador")]`,
# oculta no `NavMenu` pra quem não tem essa claim)

# Conteúdo da tela:

- Um Datagrid com os seguintes campos:
  - Login
  - Nome
  - Administrador (Sim/Não)
  - Inativo (Sim/Não)
  - Ícones de ação:
    - Alterar — abre `UsuarioDialog` (Dialogs/UsuarioDialog.razor) em modo
      edição, salva via `PUT /api/v1/usuarios/{id}`
    - Excluir — confirmação via `ShowMessageBoxAsync`, exclui via
      `DELETE /api/v1/usuarios/{id}`

  - Um header com campo de pesquisa (480px) e botão de pesquisa, que
    pesquisa por login e nome (`GET /api/v1/usuarios?busca=`), e outro botão
    para incluir um novo usuário

# Diálogo de Usuário (Incluir / Alterar)

Um único componente (`UsuarioDialog.razor`) cobre os 2 modos, controlado
pelo parâmetro `Modo` (enum `ModoDialogoUsuario`). Campos: Login, Nome,
Senha (obrigatória só no modo Incluir — em branco no Alterar mantém a senha
atual, o `UsuarioService` só recalcula o hash se vier preenchida), switch
Administrador (controla só o acesso a essa própria tela, é independente das
permissões de módulo abaixo), um switch por permissão de módulo (Empresas,
Configuração, Registros, Baixar XMLs, XMLS Baixados, Importar XMLs,
Exportar XMLs — mesmos 7 itens do `NavMenu`) e switch Inativo (desativa
sem excluir, mesmo padrão de `Empresa.Inativo`).

O usuário padrão `aggille` (semeado automaticamente na primeira subida da
API, ver `AggilleDFe.Domain/Entities/USUARIO.md`) nasce só com
Administrador marcado — só ele enxerga essa tela até que alguém libere
permissões de módulo pra outros usuários.
